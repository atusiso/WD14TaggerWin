using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace WD14TaggerWin.ModelManager
{
    public class WaifuDiffusionTaggerModel : AbstractTaggerModel
    {
        /// <summary>モデル種別</summary>
        public override modelType ModelType { get; } = modelType.WaifuDiffusionInterrogator;

        /// <summary>モデル実体</summary>
        protected InferenceSession? _session = null;

        /// <summary>
        /// リソース解放処理
        /// </summary>
        /// <param name="disposing">マネージドリソース解放フラグ</param>
        protected override void Disposed(bool disposing)
        {
            if (disposing)
            {
                // TODO: マネージ状態を破棄します (マネージ オブジェクト)。
            }

            // TODO: アンマネージ リソース (アンマネージ オブジェクト) を解放し、下の Finalize() をオーバーライドします。
            UnloadModel();

            // TODO: 大きなフィールドを null に設定します。

        }

        /// <summary>
        /// selected_tags.csvの読み込みとtagリストの作成
        /// </summary>
        /// <returns>tagリスト</returns>
        private List<string> ReadTag()
        {
            List <string> res = new List<string>();

            // 全行読み込み
            string[] rows = File.ReadAllLines(tag_file_path);
            if (rows.Length == 0) return res;

            // name列番号取得
            int nameIndex = -1;
            string[] rowData = rows[0].Split(',');
            int index = 0;
            while(index < rowData.Length)
            {
                if (rowData[index].ToUpper() == "NAME") { nameIndex = index; break; }
                index++;
            }
            if (nameIndex == -1) return res;

            // name列番号の値をリストに変換(これがtagリストとなる/推論結果のindexとtagリストのindexは一致する)
            index = 1;
            while(index < rows.Length)
            {
                if (rows[index] != string.Empty)
                {
                    rowData = rows[index].Split(',');
                    if (rowData.Length > nameIndex) res.Add(rowData[nameIndex]);
                    else res.Add(string.Empty);
                }

                index++;
            }

            return res;
        }

        /// <summary>
        /// インタロゲート実施
        /// </summary>
        /// <param name="imagePath">対象イメージファイルパス</param>
        /// <returns>結果タグ辞書</returns>
        public override (Dictionary<string, float>, Dictionary<string, float>, Dictionary<string, string>) interrogate(Image<Rgba32> image, bool isFlag)
        {
            Dictionary<string, float> ratingsRes = new Dictionary<string, float>();
            Dictionary<string, float> tagsRes = new Dictionary<string, float>();
            Dictionary<string, string> categoryRes = new Dictionary<string, string>();

            // モデル準備(ファイルのロード)
            if (IsModelLoad == false)
            {
                // この時点でキャッシュ生成に失敗している時は中断
                if (IsCacheAvail == false) return (ratingsRes, tagsRes, categoryRes);
                _session = new InferenceSession(model_file_path);
                IsModelLoad = true;
            }
            if (_session == null) return (ratingsRes, tagsRes, categoryRes);

            List<string> _tags = ReadTag();

            // モデルのイメージサイズ取得
            var firstInput = _session.InputMetadata.First().Value;

            bool mode = false;
            int width;
            int height;
            DenseTensor<float> input;

            // モデルのinput構造が変わることがあるのか不明の為一応処理(Python版ではelse側の処理を決め打ちで実施していた)
            if (firstInput.Dimensions[1] == 3)
            {
                // CHW
                height = firstInput.Dimensions[2];
                width = firstInput.Dimensions[3];
                input = new DenseTensor<float>(new[] { 1, 3, height, width });
                mode = true;
            }
            else
            {
                // HWC
                width = firstInput.Dimensions[2];
                height = firstInput.Dimensions[1];
                input = new DenseTensor<float>(new[] { 1, height, width, 3 });
            }

            // イメージをロードしてAlphaを白背景に合成、サイズをheightの正方形の中央に配置
            using (Image<Rgb24> souirceImg = ImageResizeMethods.ConvertSquareImage(image, height))
            {
                // 処理画像の確認(サイズ・センタリング・透過処理の適正チェック)
                // souirceImg.SaveAsPng(imagePath + ".png");

                // 画素配列にTensor変換
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        // Python側の処理ではBGRで処理していたので合わせる
                        if (mode)
                        {
                            // CHW
                            input[0, 0, y, x] = souirceImg[x, y].B;
                            input[0, 1, y, x] = souirceImg[x, y].G;
                            input[0, 2, y, x] = souirceImg[x, y].R;
                        }
                        else
                        {
                            // HWC
                            input[0, y, x, 0] = souirceImg[x, y].B;
                            input[0, y, x, 1] = souirceImg[x, y].G;
                            input[0, y, x, 2] = souirceImg[x, y].R;
                        }
                    }
                }
            }

            // 入力データ生成
            string inputName = _session.InputMetadata.First().Key;
            string labelName = _session.OutputMetadata.First().Key;
            var labels = new List<string> { labelName };
            var inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor(inputName, input) };

            // モデル適用
            using (var results = _session.Run(inputs))
            {
                // 結果の変換
                var output = results.First().AsEnumerable<float>().ToArray();

                // タグへの紐づけ
                int index = 0;
                foreach (string tag in _tags)
                {
                    if (index < 4) ratingsRes.Add(tag, output[index]);
                    else
                    {
                        tagsRes.Add(tag, output[index]);
                        categoryRes.Add(tag, string.Empty);
                    }
                    index++;
                }
            }

            return (ratingsRes, tagsRes, categoryRes);
        }

        /// <summary>
        /// モデルのアンロード
        /// </summary>
        public override void UnloadModel()
        {
            if (IsModelLoad)
            {
                if (_session != null) _session.Dispose();
                _session = null;
                IsModelLoad = false;
            }
        }

    }
}
