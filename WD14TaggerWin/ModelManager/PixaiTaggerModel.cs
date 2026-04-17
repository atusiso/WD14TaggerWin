using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace WD14TaggerWin.ModelManager
{
    public class PixaiTaggerModel : AbstractTaggerModel
    {
        /// <summary>モデル種別</summary>
        public override modelType ModelType { get; } = modelType.PiaiTaggerInterrogator;

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
        private void ReadTag(Dictionary<string, int> idxToTag, Dictionary<string, string> tagToCategory, Dictionary<string, List<string>> tagIps)
        {
            using (var tfp = new Microsoft.VisualBasic.FileIO.TextFieldParser(tag_file_path))
            {
                tfp.TextFieldType = Microsoft.VisualBasic.FileIO.FieldType.Delimited;
                tfp.SetDelimiters(",");
                // ヘッダ空読み
                if (!tfp.EndOfData)
                {
                    string[]? headerrow = tfp.ReadFields();
                }
                // 残りの行処理
                while (!tfp.EndOfData)
                {
                    string[]? rowData = tfp.ReadFields();
                    if ((rowData != null) && (rowData.Length > 5))
                    {
                        int idx = int.Parse(rowData[0]);
                        string tag = rowData[2];
                        string cat = rowData[3];
                        string ips = rowData[5];

                        idxToTag.Add(tag, idx);
                        // カテゴリgeneral
                        if (cat == "0")
                        {
                            tagToCategory.Add(tag, "general");
                            tagIps.Add(tag, new List<string>());
                        }
                        // カテゴリcharacter
                        else if (cat == "4")
                        {
                            tagToCategory.Add(tag, "character");
                            tagIps.Add(tag, new List<string>());

                            // characterはips列にjsonリストがある
                            try
                            {
                                using (JsonDocument doc = JsonDocument.Parse(ips))
                                {
                                    JsonElement root = doc.RootElement;
                                    if (root.ValueKind == JsonValueKind.Array)
                                    {
                                        foreach (JsonElement node in root.EnumerateArray())
                                        {
                                            if (node.ValueKind == JsonValueKind.String)
                                            {
                                                string? ip = node.GetString();
                                                if (ip != null) tagIps[tag].Add(ip);
                                            }
                                        }
                                    }
                                }
                            }
                            catch { }
                        }
                    }
                }
            }
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

            // タグの読み込みと辞書の作成
            var idxToTag = new Dictionary<string, int>();
            var tagToCategory = new Dictionary<string, string>();
            Dictionary<string, List<string>> tagIps = new Dictionary<string, List<string>>();
            ReadTag(idxToTag, tagToCategory, tagIps);

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
                height = firstInput.Dimensions[1];
                width = firstInput.Dimensions[2];
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
                            // CHW (0～1にスケーリングした後、0.5で正規化)
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
                float[]? output = null;
                foreach (var outdata in results)
                {
                    var outval = outdata.AsEnumerable<float>().ToArray();
                    if (outval.Length == idxToTag.Count)
                    {
                        output = outval;
                        break;
                    }
                }
                if (output == null) return (ratingsRes, tagsRes, categoryRes);

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

                        // カテゴリがcharacterの場合
                        if (category.ToLower() == "character")
                        {
                            // optionalThreshold以上の結果のみ登録する
                            if ((prob >= optionalThreshold))
                            {
                                // タグ結果に登録、当該タグのカテゴリを辞書に設定
                                tagsRes.Add(kvPair.Key, prob);
                                categoryRes.Add(kvPair.Key, category);

                                // キャラクタカテゴリに紐づくipカテゴリを追加
                                if (tagIps.ContainsKey(kvPair.Key))
                                {
                                    foreach (string ip in tagIps[kvPair.Key])
                                    {
                                        if (tagsRes.ContainsKey(ip) == false)
                                        {
                                            tagsRes.Add(ip, prob);
                                            categoryRes.Add(ip, "ip");
                                        }
                                        else
                                        {
                                            // 重複した場合は大きい結果を保持
                                            if (prob > tagsRes[ip])
                                            {
                                                tagsRes[ip] = prob;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        // カテゴリがcharacter以外の場合はそのまま登録
                        else
                        {
                            // タグ結果に登録、当該タグのカテゴリを辞書に設定
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
