using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace WD14TaggerWin
{
    internal class StorageSecurity
    {
        /// <summary>
        /// 管理者権限による実行か調査
        /// </summary>
        /// <returns>true...管理者権限/false...非管理者権限</returns>
        public static bool IsAdminProc()
        {
            var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            var principal = new System.Security.Principal.WindowsPrincipal(identity);
            return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        }

        /// <summary>
        /// フォルダにセキュリティ設定を与える
        /// </summary>
        /// <param name="dirName">対象フォルダ</param>
        /// <param name="account">アカウント</param>
        /// <param name="rights">権限</param>
        /// <param name="controlType">コントロール種別</param>
        public static void AddDirectorySecurity(string dirName, string account, FileSystemRights rights, AccessControlType controlType)
        {
            var dirInfo = new DirectoryInfo(dirName);
            DirectorySecurity dirSecurity = dirInfo.GetAccessControl();

            var rule = new FileSystemAccessRule(account, rights, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, controlType);
            dirSecurity.AddAccessRule(rule);
            dirInfo.SetAccessControl(dirSecurity);
        }

        /// <summary>
        /// ファイルにセキュリティ設定を与える
        /// </summary>
        /// <param name="fileName">対象ファイル</param>
        /// <param name="account">アカウント</param>
        /// <param name="rights">権限</param>
        /// <param name="controlType">コントロール種別</param>
        public static void AddFileSecurity(string fileName, string account, FileSystemRights rights, AccessControlType controlType)
        {
            var fileInfo = new FileInfo(fileName);
            FileSecurity fSecurity = fileInfo.GetAccessControl();
            var accessRule = new FileSystemAccessRule(account, rights, controlType);

            fSecurity.AddAccessRule(accessRule);
            fileInfo.SetAccessControl(fSecurity);
        }

    }
}
