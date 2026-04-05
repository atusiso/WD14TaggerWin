using System;
using System.Collections.Generic;
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
    public class CamieTaggerModel : AbstractTaggerModel
    {
        /// <summary>モデル種別</summary>
        public override modelType ModelType { get; } = modelType.CamieTaggerInterrogator;

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
                    // Jsonパース
                    using (JsonDocument doc = JsonDocument.Parse(jsonString))
                    {
                        // rootエレメント取得
                        JsonElement root = doc.RootElement;
                        // dataset_infoキーのプロパティ取得
                        JsonElement dataSetInfo;
                        if ((root.ValueKind == JsonValueKind.Object) && (root.TryGetProperty("dataset_info", out dataSetInfo)))
                        {
                            // tag_mappingキーのプロパティ取得
                            JsonElement tagMapping;
                            if ((dataSetInfo.ValueKind == JsonValueKind.Object) && (dataSetInfo.TryGetProperty("tag_mapping", out tagMapping)))
                            {
                                // idx_to_tagキーのプロパティ取得
                                JsonElement idx2Tag;
                                if ((tagMapping.ValueKind == JsonValueKind.Object) && (tagMapping.TryGetProperty("idx_to_tag", out idx2Tag)))
                                {
                                    if (idx2Tag.ValueKind == JsonValueKind.Object)
                                    {
                                        // タグをキーに結果のindexを保持する辞書を生成
                                        foreach (JsonProperty property in idx2Tag.EnumerateObject())
                                        {
                                            string key = property.Name;
                                            if (property.Value.ValueKind == JsonValueKind.String)
                                            {
                                                string? tag = property.Value.GetString();

                                                if (tag != null)
                                                {
                                                    int idx;
                                                    if ((idxToTag.ContainsKey(tag) == false) && (int.TryParse(key, out idx)))
                                                    {
                                                        idxToTag.Add(tag, idx);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }

                                // タグをキーにカテゴリを保持する辞書を生成
                                JsonElement Tag2Category;
                                if ((tagMapping.ValueKind == JsonValueKind.Object) && (tagMapping.TryGetProperty("tag_to_category", out Tag2Category)))
                                {
                                    if (Tag2Category.ValueKind == JsonValueKind.Object)
                                    {
                                        foreach (JsonProperty property in Tag2Category.EnumerateObject())
                                        {
                                            string key = property.Name;
                                            if (property.Value.ValueKind == JsonValueKind.String)
                                            {
                                                string? category = property.Value.GetString();
                                                if ((category != null) && (tagToCategory.ContainsKey(key) == false)) tagToCategory.Add(key, category);
                                            }
                                        }
                                    }
                                }

                            }
                        }
                    }
                }
                catch { }
            }

            return;
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
            // CHW
            int height = firstInput.Dimensions[2];
            int width = firstInput.Dimensions[3];
            DenseTensor<float> input = new DenseTensor<float>(new[] { 1, 3, height, width });

            // イメージをロードしてアルファを白地にサイズを短辺長をheightに合わせて拡大縮小
            using (Image<Rgb24> souirceImg = ImageResizeMethods.ConvertSquareImage2(image, height))
            {
                // 処理画像の確認(サイズ・センタリング・透過処理の適正チェック)
                // souirceImg.SaveAsPng(imagePath + ".png");

                // HWCを1CHWに変換
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        // python実装に習う
                        input[0, 0, y, x] = (souirceImg[x, y].R / 255.0f);
                        input[0, 1, y, x] = (souirceImg[x, y].G / 255.0f);
                        input[0, 2, y, x] = (souirceImg[x, y].B / 255.0f);
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
                float[] output;
                // モデルの出力結果が2以上ある場合はindex=1の結果を使う
                if (results.Count > 1)
                {
                    output = results[1].AsEnumerable<float>().ToArray();
                }
                // モデルの出力結果が1つの場合はそれを使う
                else
                {
                    output = results.First().AsEnumerable<float>().ToArray();
                }

                // タグインデックスに基づいて結果を合成
                Dictionary<string, float> allTags = new Dictionary<string, float>();
                foreach (var kvPair in idxToTag)
                {
                    // インデックスが存在する場合
                    if (kvPair.Value < output.Length)
                    {
                        // Softmax処理
                        float prob = (1.0f / (1.0f + (float)Math.Exp(-output[kvPair.Value])));
                        string category = (tagToCategory.ContainsKey(kvPair.Key) ? tagToCategory[kvPair.Key] : string.Empty);

                        // カテゴリがratingの場合はrating結果に移す
                        if (category == "rating") 
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
