using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ElBruno.HuggingFace;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace WD14TaggerWin.ModelManager
{
    public abstract class AbstractTaggerModel : IDisposable
    {
        private static string ModelDownloadCompleteFile = ".complete";

        /// <summary>
        /// ダウンロード進捗処理
        /// </summary>
        /// <param name="percent">ダウンロード率</param>
        /// <param name="isComplete">完了フラグ</param>
        /// <param name="isFailed">失敗フラグ</param>
        public delegate void DLProgress(double percent, bool isComplete, bool isFailed);

        /// <summary>モデル種別</summary>
        public enum modelType : int
        {
            WaifuDiffusionInterrogator = 0,
            MLDanbooruInterrogator = 1,
            CamieTaggerInterrogator = 2,
            ClTaggerInterrogator = 3,
            PiaiTaggerInterrogator = 4
        }

        /// <summary>モデルライセンス種別</summary>
        public enum licenseType : int
        {
            Apache2_0 = 0,
            MIT = 1,
            GPL3_0 = 2
        }

        /// <summary>モデル種別</summary>
        public abstract modelType ModelType { get; }

        /// <summary>モデル名</summary>
        public required string name { get; set; }
        /// <summary>リポジトリID</summary>
        public required string repo_id { get; set; }
        /// <summary>モデルファイルパス(リポジトリパス)</summary>
        public required string model_path { get; set; }
        /// <summary>タグファイルパス(リポジトリパス)</summary>
        public required string tag_path { get; set; }

        /// <summary>モデルライセンス</summary>
        public required licenseType license { get; set; }

        /// <summary>著者</summary>
        public required string author { get; set; }

        /// <summary>サイト</summary>
        public required string siteuri { get; set; }

        /// <summary>キャッシュチェック</summary>
        public bool IsCacheAvail { get; set; } = false;

        /// <summary>モデルロードチェック</summary>
        public bool IsModelLoad { get; set; } = false;

        /// <summary>モデルファイルパス(キャッシュファイルパス)</summary>
        protected string model_file_path { get; set; } = string.Empty;

        /// <summary>タグファイルパス(キャッシュファイルパス)</summary>
        protected string tag_file_path { get; set; } = string.Empty;

        #region IDisposable Support

        /// <summary>重複する呼び出しを検出(コードアシストによる自動生成)</summary>
        private bool disposedValue;

        /// <summary>
        /// リソース破棄
        /// </summary>
        /// <param name="disposing">マネージオブジェクトの解放指示フラグ</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                Disposed(disposing);

                if (disposing)
                {
                    // TODO: マネージ状態を破棄します (マネージ オブジェクト)。
                }

                // TODO: アンマネージ リソース (アンマネージ オブジェクト) を解放し、下の Finalize() をオーバーライドします。

                // TODO: 大きなフィールドを null に設定します。
            }
            disposedValue = true;
        }

        protected abstract void Disposed(bool disposing);

        /// <summary>
        /// ファイナライザ
        /// </summary>
        /// <remarks>TODO: 上の Dispose(ByVal disposing As Boolean) にアンマネージ リソースを解放するコードがある場合にのみ、Finalize() をオーバーライドします。</remarks>
        ~AbstractTaggerModel()
        {
            // このコードを変更しないでください。クリーンアップ コードを上の Dispose(ByVal disposing As Boolean) に記述します。
            Dispose(false);
        }

        /// <summary>
        /// リソース破棄
        /// </summary>
        /// <remarks>このコードは、破棄可能なパターンを正しく実装できるように Visual Basic によって追加されました。</remarks>
        public void Dispose()
        {
            // このコードを変更しないでください。クリーンアップ コードを上の Dispose(disposing As Boolean) に記述します。
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        /// <summary>
        /// キャッシュファイルの有無チェック
        /// </summary>
        /// <param name="key">モデルのキー(キャッシュパスのモデルパス)</param>
        /// <param name="cachePath">キャッシュルート</param>
        public void CheckCache(string key, string cachePath)
        {
            string path = Path.Combine(cachePath, key);
            string completeMark = Path.Combine(path, ModelDownloadCompleteFile);

            // ダウンロード完了ファイルがある場合モデルキャッシュ有と判断
            if (File.Exists(completeMark)) IsCacheAvail = true;
            else
            {
                // ネットワーク上のファイルと比較してダウンロード完了チェック
                using (var downloader = new HuggingFaceDownloader())
                {
                    IsCacheAvail = downloader.AreFilesAvailable([model_path, tag_path], path);
                    if (IsCacheAvail)
                    {
                        // モデルキャッシュがある場合は完了ファイルを生成(次回からネットワークに接続しない)
                        using (var fs = new FileStream(completeMark, FileMode.Create, FileAccess.Write))
                        {
                        }
                    }
                }
            }

            // キャッシュがある場合
            if (IsCacheAvail)
            {
                model_file_path = Path.Combine(path, model_path);
                tag_file_path = Path.Combine(path, tag_path);
            }
            // キャッシュファイルが無い場合
            else
            {
                model_file_path = string.Empty;
                tag_file_path = string.Empty;
            }
        }

        /// <summary>
        /// ダウンロード実施
        /// </summary>
        /// <param name="key">モデルのキー(キャッシュパスのモデルパス)</param>
        /// <param name="cachePath">キャッシュルート</param>
        /// <param name="dlProgress">ダウンロード進捗処理</param>
        public async void Download(string key, string cachePath, DLProgress dlProgress)
        {
            if (IsCacheAvail) return;

            string path = Path.Combine(cachePath, key);

            using (var downloader = new HuggingFaceDownloader())
            {
                // ダウンロード進捗処理
                var progress = new Progress<DownloadProgress>(p =>
                {
                    if (dlProgress != null)
                    {
                        // 完了/失敗チェック
                        bool isComplete = false;
                        bool isFailed = false;
                        switch (p.Stage)
                        {
                            case DownloadStage.Failed:
                                isFailed = true;
                                break;
                            case DownloadStage.Complete:
                                isComplete = true;
                                break;
                        }

                        // ダウンロード進捗処理を呼び出す
                        dlProgress(p.PercentComplete, isComplete, isFailed);
                    }
                });

                // 非同期ダウンロードの開始
                await downloader.DownloadFilesAsync(
                    new DownloadRequest { RepoId = repo_id, LocalDirectory = path, RequiredFiles = [model_path, tag_path], Progress = progress }
                );
            }
        }

        /// <summary>
        /// インタロゲート実施
        /// </summary>
        /// <param name="souirceImg">対象イメージ</param>
        /// <returns>結果タグ辞書</returns>
        public abstract (Dictionary<string, float>, Dictionary<string, float>, Dictionary<string, string>) interrogate(Image<Rgba32> image, bool isFlag, float optionalThreshold);

        /// <summary>
        /// モデルのアンロード
        /// </summary>
        public abstract void UnloadModel();
    }
}
