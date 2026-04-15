using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Controls;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace WD14TaggerWin.ModelManager
{
    public class ClTaggerModel : AbstractTaggerModel
    {
        /// <summary>モデル種別</summary>
        public override modelType ModelType { get; } = modelType.ClTaggerInterrogator;

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
        /// <param name="idxToTag"></param>
        /// <param name="tagToCategory"></param>
        private void ReadTag(Dictionary<string, int> idxToTag, Dictionary<string, string> tagToCategory)
        {
            // タグファイルのjsonを読み込む
            using (FileStream jsonFile = new FileStream(tag_file_path, FileMode.Open, FileAccess.Read))
            using (StreamReader reader = new StreamReader(jsonFile, Encoding.UTF8))
            {
                // ファイルを読み込む
                string jsonString = reader.ReadToEnd();
                try
                {
                    using (JsonDocument doc = JsonDocument.Parse(jsonString))
                    {
                        // rootエレメント取得
                        JsonElement root = doc.RootElement;
                        if (root.ValueKind == JsonValueKind.Object)
                        {
                            // root辞書をループ
                            foreach (JsonProperty property in root.EnumerateObject())
                            {
                                // keyがindex、valueがJsonValueKind.Object
                                if (property.Value.ValueKind == JsonValueKind.Object)
                                {
                                    int index = int.Parse(property.Name);
                                    JsonElement tag;
                                    if ((property.Value.TryGetProperty("tag", out tag)) && (tag.ValueKind == JsonValueKind.String))
                                    {
                                        string? tagName = tag.GetString();

                                        JsonElement category;
                                        string categoryName = "Unknown";
                                        if ((property.Value.TryGetProperty("category", out category)) && (category.ValueKind == JsonValueKind.String))
                                        {
                                            categoryName = category.GetString() ?? "Unknown";
                                        }

                                        if (tagName != null)
                                        {
                                            idxToTag.Add(tagName, index);
                                            tagToCategory.Add(tagName, categoryName);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch { }
            }
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

            // タグの読み込みと辞書の作成
            var idxToTag = new Dictionary<string, int>();
            var tagToCategory = new Dictionary<string, string>();
            ReadTag(idxToTag, tagToCategory);

            // モデルのイメージサイズ取得
            var firstInput = _session.InputMetadata.First().Value;

            bool mode = false;
            int width = 448;
            int height = 448;
            DenseTensor<float> input;

            // モデルのinput構造が変わることがあるのか不明の為一応処理(Python版ではelse側の処理を決め打ちで実施していた)
            if (firstInput.Dimensions[1] == 3)
            {
                // CHW
                input = new DenseTensor<float>(new[] { 1, 3, height, width });
                mode = true;
            }
            else
            {
                // HWC
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
                            // CHW (0～1に正規化した後、-1～1で正規化)
                            input[0, 0, y, x] = (souirceImg[x, y].B / 255.0f - 0.5f) / 0.5f;
                            input[0, 1, y, x] = (souirceImg[x, y].G / 255.0f - 0.5f) / 0.5f;
                            input[0, 2, y, x] = (souirceImg[x, y].R / 255.0f - 0.5f) / 0.5f;
                        }
                        else
                        {
                            // HWC
                            input[0, y, x, 0] = (souirceImg[x, y].B / 255.0f - 0.5f) / 0.5f;
                            input[0, y, x, 1] = (souirceImg[x, y].G / 255.0f - 0.5f) / 0.5f;
                            input[0, y, x, 2] = (souirceImg[x, y].R / 255.0f - 0.5f) / 0.5f;
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

                // タグインデックスに基づいて結果を合成
                Dictionary<string, float> allTags = new Dictionary<string, float>();
                foreach (var kvPair in idxToTag)
                {
                    // インデックスが存在する場合
                    if (kvPair.Value < output.Length)
                    {
                        // NANの場合0.0 無限の場合0～1に収束
                        if (float.IsNaN(output[kvPair.Value])) output[kvPair.Value] = 0.0f;
                        if (float.IsInfinity(output[kvPair.Value])) output[kvPair.Value] = 1.0f;
                        if (float.IsNegativeInfinity(output[kvPair.Value])) output[kvPair.Value] = 0.0f;

                        // Softmax処理
                        float prob = (1.0f / (1.0f + (float)Math.Exp(-Math.Clamp(output[kvPair.Value], -30, 30))));
                        string category = (tagToCategory.ContainsKey(kvPair.Key) ? tagToCategory[kvPair.Key] : string.Empty);

                        // カテゴリがratingの場合はrating結果に移す
                        if (category.ToLower() == "rating")
                        {
                            ratingsRes.Add(kvPair.Key, prob);
                        }
                        // それ以外はタグ結果に登録、当該タグのカテゴリを辞書に設定
                        else
                        {
                            tagsRes.Add(kvPair.Key, prob);
                            categoryRes.Add(kvPair.Key, category);
                        }
                    }
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
