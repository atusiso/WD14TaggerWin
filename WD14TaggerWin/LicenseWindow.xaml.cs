using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WD14TaggerWin
{
    /// <summary>
    /// LicenseWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class LicenseWindow : Window
    {

        /// <summary>制御対象のExpander一覧</summary>
        List<Expander> LibExpanders = new List<Expander>();
        /// <summary>制御Expanderの内容TextBox辞書</summary>
        Dictionary<Expander, TextBox> LibExpanderDic = new Dictionary<Expander, TextBox>();

        /// <summary>Expanderの閉じたときのサイズ</summary>
        public double LibExpanderUnitHeight = 0;
        /// <summary>Expanderの内容物サイズ</summary>
        public double LibInnerHeight = 0;

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
        /// モデルコンテンツの追加
        /// </summary>
        /// <param name="modelName"></param>
        /// <param name="auther"></param>
        /// <param name="licenseType"></param>
        /// <param name="uri"></param>
        public void AddModelContent(string modelName, string auther, ModelManager.AbstractTaggerModel.licenseType licenseType, string uri)
        {
            // タイトルコントロール作成
            var titleLabel = new Label();
            titleLabel.Content = modelName;
            titleLabel.HorizontalAlignment = HorizontalAlignment.Left;
            titleLabel.VerticalAlignment = VerticalAlignment.Top;
            titleLabel.FontSize += 2.0;
            titleLabel.FontWeight = FontWeights.Bold;
            titleLabel.Margin = new Thickness(10, 0, 0, 0);
            ModelParent.Children.Add(titleLabel);

            // 作者コントロール作成
            var autherLabel = new Label();
            autherLabel.Content = "作者：" + auther;
            autherLabel.HorizontalAlignment = HorizontalAlignment.Left;
            autherLabel.VerticalAlignment = VerticalAlignment.Top;
            autherLabel.Margin = new Thickness(20, 0, 0, 0);
            ModelParent.Children.Add(autherLabel);

            // ライセンス種別コントロール作成
            var licenseLabel = new Label();
            switch (licenseType)
            {
                case ModelManager.AbstractTaggerModel.licenseType.Apache2_0:
                    licenseLabel.Content = "ライセンス：Apache 2.0";
                    break;
                case ModelManager.AbstractTaggerModel.licenseType.MIT:
                    licenseLabel.Content = "ライセンス：MIT";
                    break;
                case ModelManager.AbstractTaggerModel.licenseType.GPL3_0:
                    licenseLabel.Content = "ライセンス：GPL 3.0";
                    break;
                default:
                    licenseLabel.Content = "ライセンス：不明";
                    break;
            }
            licenseLabel.HorizontalAlignment = HorizontalAlignment.Left;
            licenseLabel.VerticalAlignment = VerticalAlignment.Top;
            licenseLabel.Margin = new Thickness(20, 0, 0, 0);
            ModelParent.Children.Add(licenseLabel);

            // サイトURIコントロール作成
            var uriTextblock = new TextBlock();
            var run = new Run();
            run.Text = "hugging face site：";
            uriTextblock.Inlines.Add(run);
            var uriHyperLink = new Hyperlink();
            uriHyperLink.Inlines.Add(uri);
            uriHyperLink.NavigateUri = new Uri(uri);
            uriHyperLink.RequestNavigate += Hyperlink_RequestNavigate;
            uriTextblock.Inlines.Add(uriHyperLink);
            uriTextblock.Margin = new Thickness(20, 0, 0, 10);
            ModelParent.Children.Add(uriTextblock);
        }

        /// <summary>
        /// フォームロード
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LibExpanders.Add(LibExpander1);
            LibExpanders.Add(LibExpander2);
            LibExpanders.Add(LibExpander3);
            LibExpanders.Add(LibExpander4);
            LibExpanders.Add(LibExpander5);
            LibExpanderDic.Add(LibExpander1, LibExpanderInner1);
            LibExpanderDic.Add(LibExpander2, LibExpanderInner2);
            LibExpanderDic.Add(LibExpander3, LibExpanderInner3);
            LibExpanderDic.Add(LibExpander4, LibExpanderInner4);
            LibExpanderDic.Add(LibExpander5, LibExpanderInner5);

            LibExpanderUnitHeight = LibExpander1.ActualHeight;
            LibInnerHeight = ParentGrid_Lib.ActualHeight - LibExpanderUnitHeight * LibExpanders.Count;
        }

        /// <summary>
        /// サイズ変更
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            LibInnerHeight = ParentGrid_Lib.ActualHeight - LibExpanderUnitHeight * LibExpanders.Count;
            ResizeInnerLib();
        }

        /// <summary>
        /// Expander展開
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LibExpander_Expanded(object sender, RoutedEventArgs e)
        {
            // 展開は常に一つ
            foreach (Expander expander in LibExpanders)
            {
                if (expander != sender) expander.IsExpanded = false;
            }
            ResizeInnerLib();
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
        /// ライブラリ内容物のサイズ変更
        /// </summary>
        private void ResizeInnerLib()
        {
            foreach (Expander expander in LibExpanders)
            {
                if (expander.IsExpanded) LibExpanderDic[expander].Height = LibInnerHeight;
                else LibExpanderDic[expander].Height = 0;
            }
        }

        /// <summary>
        /// サイトURIのクリック
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            // 規定のブラウザでURLを開く
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }

    }
}
