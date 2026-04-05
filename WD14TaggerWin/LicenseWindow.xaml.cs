using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WD14TaggerWin
{
    /// <summary>
    /// LicenseWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class LicenseWindow : Window
    {

        /// <summary>制御対象のExpander一覧</summary>
        List<Expander> Expanders = new List<Expander>();
        /// <summary>制御Expanderの内容TextBox辞書</summary>
        Dictionary<Expander, TextBox> ExpanderDic = new Dictionary<Expander, TextBox>();

        /// <summary>Expanderの閉じたときのサイズ</summary>
        public double ExpanderUnitHeight = 0;
        /// <summary>Expanderの内容物サイズ</summary>
        public double InnerHeight = 0;

        public LicenseWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// スケール変更
        /// </summary>
        /// <param name="scale"></param>
        public void InitScale(double scale)
        {
            Scale.ScaleX = scale;
            Scale.ScaleY = scale;

            MinWidth = MinWidth * scale;
            MinHeight = MinHeight * scale;
            Width = Width * scale;
            Height = Height * scale;
        }

        /// <summary>
        /// フォームロード
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Expanders.Add(Expander1);
            Expanders.Add(Expander2);
            Expanders.Add(Expander3);
            Expanders.Add(Expander4);
            Expanders.Add(Expander5);
            ExpanderDic.Add(Expander1, ExpanderInner1);
            ExpanderDic.Add(Expander2, ExpanderInner2);
            ExpanderDic.Add(Expander3, ExpanderInner3);
            ExpanderDic.Add(Expander4, ExpanderInner4);
            ExpanderDic.Add(Expander5, ExpanderInner5);

            ExpanderUnitHeight = Expander1.ActualHeight;
            InnerHeight = ParentGrid.ActualHeight - ExpanderUnitHeight * Expanders.Count;
        }

        /// <summary>
        /// サイズ変更
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            InnerHeight = ParentGrid.ActualHeight - ExpanderUnitHeight * Expanders.Count;
            ResizeInner();
        }

        /// <summary>
        /// Expander展開
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Expander_Expanded(object sender, RoutedEventArgs e)
        {
            // 展開は常に一つ
            foreach (Expander expander in Expanders)
            {
                if (expander != sender) expander.IsExpanded = false;
            }
            ResizeInner();
        }

        /// <summary>
        /// OKボタン押下
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// 内容物のサイズ変更
        /// </summary>
        private void ResizeInner()
        {
            foreach (Expander expander in Expanders)
            {
                if (expander.IsExpanded) ExpanderDic[expander].Height = InnerHeight;
                else ExpanderDic[expander].Height = 0;
            }
        }

    }
}
