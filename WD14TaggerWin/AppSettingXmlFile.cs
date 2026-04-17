using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace WD14TaggerWin
{
    [Serializable()]
    public class AppSettingXmlFile : XMLSerializableClassBase
    {
        // |----------------------------------------------------------------------------------------------------------
        // | メンバ宣言 (保存対象)
        // |----------------------------------------------------------------------------------------------------------
        /// <summary>設定ファイルの作成マシン名</summary>
        public string ConfigMachineName = string.Empty;
        // ----------------------------------------------------------------------------------------------------------
        /// <summary>モデルキャッシュパス</summary>
        public string CachePath = string.Empty;
        /// <summary>選択インタロゲータ</summary>
        public string Interrogator = string.Empty;
        /// <summary>閾値</summary>
        public string Threshold = "0.35";
        /// <summary>閾値2</summary>
        public string Threshold2 = "0.8";
        // ----------------------------------------------------------------------------------------------------------
        /// <summary>推論結果タグのアンダーバーをスペースに変換する</summary>
        public bool IsUnderScoreToSpace = true;
        /// <summary>推論結果タグのカッコをエスケープする</summary>
        public bool IsTagBracketsEscape = true;
        /// <summary>ファイルドロップでタグ推論開始</summary>
        public bool IsDropToTagging = false;
        /// <summary>推論結果タグをクリップボードにコピー</summary>
        public bool IsReslutToClipbord = false;
        /// <summary>MDDanbooruの画像縮小処理を変更する</summary>
        public bool IsMLDanbooruResizeNew = false;
        // ----------------------------------------------------------------------------------------------------------
        /// <summary>ウィンドウの座標を覚える</summary>
        public bool IsWinPosMemory = true;
        /// <summary>ウィンドウの上位置</summary>
        public double WindowTop = -1;
        /// <summary>ウィンドウの左位置</summary>
        public double WindowLeft = -1;
        /// <summary>ウィンドウの横幅</summary>
        public double WindowWidth = 882;
        /// <summary>ウィンドウの縦幅</summary>
        public double WindowHeight = 793;

        // |----------------------------------------------------------------------------------------------------------
        // | メンバ宣言 (保存対象外)
        // |----------------------------------------------------------------------------------------------------------

        /// <summary>
        /// 設定ファイル名
        /// </summary>
        [System.Xml.Serialization.XmlIgnore()]
        protected string SaveFileName = "AppSettingXmlFile.xml";
        /// <summary>
        /// アプリケーション名
        /// </summary>
        [System.Xml.Serialization.XmlIgnore()]
        protected string? AppricationName = "WD14TaggerWin";

        // |----------------------------------------------------------------------------------------------------------
        // | プロパティ
        // |----------------------------------------------------------------------------------------------------------

        /// <summary>ファイル名</summary>
        [XmlIgnoreAttribute()]
        public override string FileName
        {
            get
            {
                return SaveFileName;
            }
            set
            {
                SaveFileName = value;
            }
        }

        /// <summary>ファイルパス</summary>
        [XmlIgnoreAttribute()]
        public override string FilePath { get; set; } = string.Empty;

        // |----------------------------------------------------------------------------------------------------------
        // | コンストラクタ/デストラクタ宣言
        // |----------------------------------------------------------------------------------------------------------

        /// <summary>
        /// デフォルトコンストラクタ
        /// </summary>
        public AppSettingXmlFile()
        {
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="appricationName"></param>
        /// <param name="configFileName"></param>
        /// <param name="targetPath"></param>
        /// <param name="loadAndCreate"></param>
        public AppSettingXmlFile(string appricationName = "WD14TaggerWin", string? configFileName = null, string? targetPath = null, bool loadAndCreate = false)
        {
            // マシン名称を取得
            string strMacName = Environment.MachineName.ToLower();

            // 引数をローカル変数に設定
            ConfigMachineName = strMacName;
            AppricationName = appricationName;

            // パス指定が無い場合、デフォルトパスを設定
            if (targetPath is null)
            {
                // 会社名\アプリケーション名\config\
                string basePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                FilePath = System.IO.Path.Combine(basePath, @"AS\" + AppricationName + @"\config");
            }
            else
            {
                FilePath = targetPath;
            }
            if (configFileName != null)
            {
                FileName = configFileName;
            }

            InitConfig(loadAndCreate);
        }

        // |----------------------------------------------------------------------------------------------------------
        // | メソッド
        // |----------------------------------------------------------------------------------------------------------

        /// <summary>
        /// 初期化
        /// </summary>
        /// <param name="loadAndCreate"></param>
        /// <returns></returns>
        public bool InitConfig(bool loadAndCreate = false)
        {
            bool res = false;

            if (loadAndCreate)
            {
                // 設定ファイルが存在する場合、読み込む
                string fileName = System.IO.Path.Combine(FilePath, FileName);
                if (System.IO.File.Exists(fileName))
                {
                    RefreshFromFile();
                    res = true;
                }
                else
                {
                    // 初期化
                    Init();

                    // 更新
                    UpdateToFile();
                }
            }
            else
            {
                // 初期化
                Init();
            }

            return res;
        }

        /// <summary>
        /// 初期化
        /// </summary>
        public override void Init()
        {
            if (CachePath == string.Empty)
            {
                string basePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                CachePath = System.IO.Path.Combine(basePath, @"AS\" + AppricationName + @"\model");
            }
        }

        /// <summary>
        /// インスタンスからメンバをコピー
        /// </summary>
        /// <param name="src">コピー元インスタンス</param>
        /// <param name="forceCopy">強制コピーフラグ(安全領域のメンバもコピーするフラグ)</param>
        public override void CopyFrom(object src, bool forceCopy = false)
        {
            AppSettingXmlFile srcObj = (AppSettingXmlFile)src;

            // 通常メンバ
            CachePath = srcObj.CachePath;
            Interrogator = srcObj.Interrogator;
            Threshold = srcObj.Threshold;
            Threshold2 = srcObj.Threshold2;

            IsUnderScoreToSpace = srcObj.IsUnderScoreToSpace;
            IsTagBracketsEscape = srcObj.IsTagBracketsEscape;
            IsDropToTagging = srcObj.IsDropToTagging;
            IsReslutToClipbord = srcObj.IsReslutToClipbord;
            IsMLDanbooruResizeNew = srcObj.IsMLDanbooruResizeNew;

            IsWinPosMemory = srcObj.IsWinPosMemory;
            WindowTop = srcObj.WindowTop;
            WindowLeft = srcObj.WindowLeft;
            WindowWidth = srcObj.WindowWidth;
            WindowHeight = srcObj.WindowHeight;

            // マシン固有安全領域情報
            if ((srcObj.ConfigMachineName == ConfigMachineName) || (forceCopy))
            {
            }
        }

        /// <summary>
        /// 非参照コピーの生成
        /// </summary>
        /// <returns>非参照コピー(スレッドセーフコピー)</returns>
        public override object DeepCopy()
        {
            AppSettingXmlFile res = new AppSettingXmlFile();

            // 通常メンバ
            res.ConfigMachineName = ConfigMachineName;
            res.CachePath = CachePath;
            res.Interrogator = Interrogator;
            res.Threshold = Threshold;
            res.Threshold2 = Threshold2;

            res.IsUnderScoreToSpace = IsUnderScoreToSpace;
            res.IsTagBracketsEscape = IsTagBracketsEscape;
            res.IsDropToTagging = IsDropToTagging;
            res.IsReslutToClipbord = IsReslutToClipbord;
            res.IsMLDanbooruResizeNew = IsMLDanbooruResizeNew;

            res.IsWinPosMemory = IsWinPosMemory;
            res.WindowTop = WindowTop;
            res.WindowLeft = WindowLeft;
            res.WindowWidth = WindowWidth;
            res.WindowHeight = WindowHeight;

            return res;
        }
    }
}