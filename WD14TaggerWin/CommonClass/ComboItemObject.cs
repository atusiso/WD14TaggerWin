using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WD14TaggerWin
{
    internal class ComboItemObject
    {
        /// <summary>
        /// 表示値
        /// </summary>
        private string _title = "";
        /// <summary>
        /// 内部値
        /// </summary>
        private string _Value = "";

        /// <summary>
        /// 表示値
        /// </summary>
        /// <value>表示値</value>
        /// <returns>表示値</returns>
        public string Title
        {
            get
            {
                return _title;
            }
            set
            {
                _title = value;
            }
        }

        /// <summary>
        /// 内部値
        /// </summary>
        /// <value>内部値</value>
        /// <returns>内部値</returns>
        public string Value
        {
            get
            {
                return _Value;
            }
            set
            {
                _Value = value;
            }
        }

        /// <summary>
        /// オーバーライドしたToString。表示値を返す
        /// </summary>
        /// <returns>表示値</returns>
        public override string ToString()
        {
            return _title;
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="Title">表示値</param>
        /// <param name="val1">内部値</param>
        public ComboItemObject(string Title, string val1)
        {
            _title = Title;
            _Value = val1;
        }
    }
}
