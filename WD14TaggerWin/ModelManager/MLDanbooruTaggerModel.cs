using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace WD14TaggerWin.ModelManager
{
    public class MLDanbooruTaggerModel : AbstractTaggerModel
    {
        /// <summary>モデル種別</summary>
        public override modelType ModelType { get; } = modelType.MLDanbooruInterrogator;

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
        /// タグの読み込み
        /// </summary>
        /// <returns>tagリスト</returns>
        private List<string> ReadTag()
        {
            List<string> res = new List<string>();

            using (FileStream jsonFile = new FileStream(tag_file_path, FileMode.Open, FileAccess.Read))
            using (StreamReader reader = new StreamReader(jsonFile, Encoding.UTF8))
            {
                string jsonString = reader.ReadToEnd();

                try
                {
                    // Jsonパース
                    using (JsonDocument doc = JsonDocument.Parse(jsonString))
                    {
                        // rootエレメント取得/ルートが配列の場合
                        JsonElement root = doc.RootElement;
                        if (root.ValueKind == JsonValueKind.Array)
                        {
                            // 配列要素を順番にタグ一覧に登録
                            foreach (var property in root.EnumerateArray())
                            {
                                if (property.ValueKind == JsonValueKind.String)
                                {
                                    string? tag = property.GetString();
                                    if (tag != null) res.Add(tag);
                                }
                            }
                        }
                    }
                }
                catch { }
            }

            return res;
        }

        /// <summary>
        /// インタロゲート実施
        /// </summary>
        /// <param name="imagePath">対象イメージファイルパス</param>
        /// <returns>結果タグ辞書</returns>
        public override (Dictionary<string, float>, Dictionary<string, float>, Dictionary<string, string>) interrogate(Image<Rgba32> image, bool isFlag, float optionalThreshold)
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
            int width = 448;    // ml-danbooru-onnxのモデル説明の標準に合わせる
            int height = 448;
            DenseTensor<float> input = new DenseTensor<float>(new[] { 1, 3, height, width });

            // イメージをロードしてアルファを白地にサイズを短辺長をheightに合わせて拡大縮小
            using (Image<Rgb24> souirceImg = (isFlag ? ImageResizeMethods.ConvertSquareImage(image, height) : ImageResizeMethods.ConvertMinsizeImage(image, height)))
            {
                // 処理画像の確認(サイズ・センタリング・透過処理の適正チェック)
                // souirceImg.SaveAsPng(imagePath + ".png");

                // HWCを1CHWに変換
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        // python実装に習う(0～1のスケーリング)
                        input[0, 0, y, x] = (souirceImg[x, y].R / 255.0f);
                        input[0, 1, y, x] = (souirceImg[x, y].G / 255.0f);
                        input[0, 2, y, x] = (souirceImg[x, y].B / 255.0f);

                        // modelの説明見るとImageNetの平均を引いて偏差で割る処理が必要？
                        //input[0, 0, y, x] = (souirceImg[x, y].R / 255.0f - 0.485f) / 0.299f;
                        //input[0, 1, y, x] = (souirceImg[x, y].G / 255.0f - 0.456f) / 0.244f;
                        //input[0, 2, y, x] = (souirceImg[x, y].B / 255.0f - 0.406f) / 0.255f;
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
                    // Softmax処理
                    tagsRes.Add(tag, 1.0f / (1.0f + (float)Math.Exp(-output[index])));
                    categoryRes.Add(tag, string.Empty);
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
