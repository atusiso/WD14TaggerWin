using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace WD14TaggerWin
{
    [Serializable()]
    public abstract class XMLSerializableClassBase
    {
        // |-----------------------------------------------------------------------------
        // | プロパティ(シリアライズ対象外)
        // |-----------------------------------------------------------------------------

        /// <summary>ファイル名</summary>
        [XmlIgnoreAttribute()]
        public abstract string FileName { get; set; }

        /// <summary>ファイルパス</summary>
        [XmlIgnoreAttribute()]
        public abstract string FilePath { get; set; }

        // |-----------------------------------------------------------------------------
        // | シリアライズメソッド
        // |-----------------------------------------------------------------------------

        /// <summary>
        /// 基準フォルダの設定
        /// </summary>
        /// <param name="basePath">保存先パス</param>
        public virtual void SetBasePath(string basePath)
        {
            // 保存先パス
            FilePath = basePath;
        }

        /// <summary>
        /// 設定ファイルの有無チェック
        /// </summary>
        /// <returns></returns>
        public virtual bool CheckPath()
        {
            string fileName = Path.Combine(FilePath, FileName);
            return File.Exists(fileName);
        }

        /// <summary>
        /// XMLファイルから自インスタンスを復元
        /// </summary>
        /// <param name="fileName">ファイル名(フルパス)</param>
        public virtual void RefreshFromFile()
        {
            string fileName = Path.Combine(FilePath, FileName);

            Type targetType = this.GetType();
            XmlSerializer serializer = new XmlSerializer(targetType);

            if (File.Exists(fileName))
            {
                object? instance = null;
                using (FileStream xmlfileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    instance = serializer.Deserialize(xmlfileStream);
                }
                if (instance != null)CopyFrom(instance);
            }
        }

        /// <summary>
        /// 自インスタンスをXMLファイルに変換
        /// </summary>
        public virtual void UpdateToFile()
        {
            // パスが無い場合、フォルダを生成
            if (Directory.Exists(FilePath) == false)
            {
                try
                {
                    var DirInfo = Directory.CreateDirectory(FilePath);
                    StorageSecurity.AddDirectorySecurity(FilePath, "Users", FileSystemRights.FullControl, AccessControlType.Allow);
                }
                catch (Exception ex)
                {
                    throw new IOException("設定ファイルの初期保存パスが生成出来ませんでした。", ex);
                }
            }

            // 保存ファイル名
            string fileName = Path.Combine(FilePath, FileName);
            string tmpName = fileName + ".tmp";
            string backName = fileName + ".back";

            // シリアライズ用型情報取得
            Type targetType = this.GetType();
            XmlSerializer serializer = new XmlSerializer(targetType);

            // 一時ファイルと指定出力
            using (FileStream xmlfileStream = new FileStream(tmpName, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                serializer.Serialize(xmlfileStream, this);
                xmlfileStream.Flush();
            }
            StorageSecurity.AddFileSecurity(tmpName, "Users", FileSystemRights.FullControl, AccessControlType.Allow);

            // 実ファイルに置き換え           
            if (File.Exists(fileName))
            {
                // 存在する場合は置き換え
                File.Replace(tmpName, fileName, backName);
            }
            else
            {
                // 存在しない場合はリネーム
                File.Move(tmpName, fileName);
            }
        }

        // |-----------------------------------------------------------------------------
        // | 継承メソッド
        // |-----------------------------------------------------------------------------

        /// <summary>
        /// 初期化
        /// </summary>
        public abstract void Init();

        /// <summary>
        /// インスタンスからメンバをコピー
        /// </summary>
        /// <param name="src">コピー元インスタンス</param>
        /// <param name="forceCopy">強制コピーフラグ(安全領域のメンバもコピーするフラグ)</param>
        public abstract void CopyFrom(object src, bool forceCopy = false);

        /// <summary>
        /// 非参照コピーの生成
        /// </summary>
        /// <returns>非参照コピー(スレッドセーフコピー)</returns>
        public abstract object DeepCopy();
    }
}
