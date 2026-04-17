using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using SixLabors.ImageSharp;

namespace WD14TaggerWin.ModelManager
{
    internal class ResultTagSet : IDisposable
    {
        /// <summary>推論対象画像パス</summary>
        public string TargetImagePath = string.Empty;
        /// <summary>推論対象画像実体</summary>
        public SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>? TagetImage = null;

        /// <summary>推論結果：レーティング結果</summary>
        public Dictionary<string, float> ratingsRes = new();
        /// <summary>推論結果：タグ結果</summary>
        public Dictionary<string, float> tagsRes = new();
        /// <summary>推論結果：カテゴリ結果</summary>
        public Dictionary<string, string> categoryRes = new();

        /// <summary>推論結果：カテゴリ一覧</summary>
        private List<string> categorys = new();

        /// <summary>閾値適用結果：カテゴリ辞書</summary>
        public Dictionary<string, List<string>> categoryDic = new();
        /// <summary>閾値適用結果：タグ辞書</summary>
        public Dictionary<string, float> resultTagDic = new();

        /// <summary>リソース破棄制御フラグ</summary>
        private bool disposedValue;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public ResultTagSet()
        {
        }

        #region "IDisposable"
        /// <summary>
        /// リソースの破棄
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: マネージド状態を破棄します (マネージド オブジェクト)
                }

                // TODO: アンマネージド リソース (アンマネージド オブジェクト) を解放し、ファイナライザーをオーバーライドします
                if (TagetImage != null)
                {
                    // 以前の画像を解放
                    TagetImage.Dispose();
                    TagetImage = null;
                }

                // TODO: 大きなフィールドを null に設定します
                disposedValue = true;
            }
        }

        /// <summary>
        /// ファイナライザ
        /// </summary>
        ~ResultTagSet()
        {
            // このコードを変更しないでください。クリーンアップ コードを 'Dispose(bool disposing)' メソッドに記述します
            Dispose(disposing: false);
        }

        /// <summary>
        /// リソースの破棄
        /// </summary>
        public void Dispose()
        {
            // このコードを変更しないでください。クリーンアップ コードを 'Dispose(bool disposing)' メソッドに記述します
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion

        /// <summary>
        /// イメージのロードを実施
        /// </summary>
        /// <param name="filePath">ロードファイルパス</param>
        /// <returns>画面表示用BitmapSource</returns>
        public BitmapSource LoadImage(string filePath)
        {
            // 対象イメージをロード
            if (TagetImage != null)
            {
                // 以前の画像を解放
                TagetImage.Dispose();
                TagetImage = null;
            }
            TagetImage = SixLabors.ImageSharp.Image.Load<SixLabors.ImageSharp.PixelFormats.Rgba32>(filePath);

            // イメージのパスを保持
            TargetImagePath = filePath;

            // 読み込み画像をBitmapSourceに変換して応答
            var stream = new System.IO.MemoryStream();
            TagetImage.SaveAsBmp(stream);
            stream.Seek(0, System.IO.SeekOrigin.Begin);
            return System.Windows.Media.Imaging.BitmapFrame.Create(
                    stream,
                    System.Windows.Media.Imaging.BitmapCreateOptions.None,
                    System.Windows.Media.Imaging.BitmapCacheOption.OnLoad
                );
        }

        /// <summary>
        /// 結果消去
        /// </summary>
        public void InitResult() 
        {
            ratingsRes.Clear();
            tagsRes.Clear();
            categoryRes.Clear();

            InitViewResult();
        }

        /// <summary>
        /// 結果登録
        /// </summary>
        /// <param name="interrogateResult">推論結果</param>
        public void SetResult((Dictionary<string, float>, Dictionary<string, float>, Dictionary<string, string>) interrogateResult)
        {
            InitViewResult();

            ratingsRes = interrogateResult.Item1;
            tagsRes = interrogateResult.Item2;
            categoryRes = interrogateResult.Item3;

            categorys.Clear();
            categorys.Add("* All *");
            foreach (string category in categoryRes.Values)
            {
                if (categorys.Contains(category) == false) categorys.Add(category);
            }
        }

        /// <summary>
        /// 表示結果初期化
        /// </summary>
        public void InitViewResult()
        {
            // 閾値適用結果を一度消去
            categoryDic.Clear();
            categoryDic.Add("* All *", new List<string>());
            resultTagDic.Clear();
        }

        /// <summary>
        /// 閾値適用
        /// </summary>
        /// <param name="threshold">閾値</param>
        /// <param name="isUnderscoreToSpace">アンダーバーを空白に</param>
        /// <param name="isTagBracketsEscape">括弧をエスケープ</param>
        public void ApplyThreshold(float threshold, bool isUnderscoreToSpace, bool isTagBracketsEscape)
        {
            // 結果タグ一覧から閾値順にソート
            var sortedDict = tagsRes.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
            foreach (var kvPair in sortedDict)
            {
                // 閾値以上のタグのみに絞る
                if (kvPair.Value > threshold)
                {
                    // エスケープ処理
                    string tag = kvPair.Key;
                    if (isUnderscoreToSpace) tag = tag.Replace('_', ' ');
                    if (isTagBracketsEscape)
                    {
                        tag = tag.Replace("(", "\\(");
                        tag = tag.Replace(")", "\\)");
                    }
                    // 結果タグリストに登録
                    resultTagDic.Add(tag, kvPair.Value);

                    // カテゴリ辞書に登録
                    categoryDic["* All *"].Add(tag);
                    string category = (categoryRes.ContainsKey(kvPair.Key) ? categoryRes[kvPair.Key] : string.Empty);
                    if (category != string.Empty)
                    {
                        if (categoryDic.ContainsKey(category) == false)
                        {
                            categoryDic.Add(category, new List<string>());
                        }
                        categoryDic[category].Add(tag);
                    }
                }
            }
        }

        /// <summary>
        /// カテゴリ一覧取得
        /// </summary>
        /// <returns>カテゴリ一覧</returns>
        public List<string> GetCategories()
        {
            return categorys;
        }

        /// <summary>
        /// 指定カテゴリのタグ一覧取得
        /// </summary>
        /// <param name="category">カテゴリ</param>
        /// <returns></returns>
        public List<string> GetCategoryTags(string category)
        {
            if (categoryDic.ContainsKey (category) == false)return new List<string>();
            return categoryDic[category];
        }

    }
}
