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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WD14TaggerWin
{
    /// <summary>
    /// ImageInfoPrompt.xaml の相互作用ロジック
    /// </summary>
    public partial class ImageInfoPrompt : UserControl
    {
        /// <summary>制御対象のExpander一覧</summary>
        List<Expander> Expanders = new List<Expander>();
        /// <summary>制御Expanderの内容TextBox辞書</summary>
        Dictionary<Expander, Grid> ExpanderHeightDic = new Dictionary<Expander, Grid>();
        /// <summary>制御Expanderの内容TextBox辞書</summary>
        Dictionary<Expander, TextBox> ExpanderDic = new Dictionary<Expander, TextBox>();

        /// <summary>Expanderの閉じたときのサイズ</summary>
        public double ExpanderUnitHeight = 0;
        /// <summary>Expanderの内容物サイズ</summary>
        public double InnerHeight = 0;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public ImageInfoPrompt()
        {
            InitializeComponent();
        }

        /// <summary>
        /// コンテンツの設定
        /// </summary>
        /// <param name="title">タイトル</param>
        /// <param name="positivePrompt">ポジティブプロンプト</param>
        /// <param name="negativePrompt">ネガティブプロンプト</param>
        /// <param name="promptAll">プロンプト全体</param>
        public void SetContents (string title, string positivePrompt, string negativePrompt, string promptAll)
        {
            // タイトルを設定
            TitleLabel.Text = "Image Info " + title;

            // プロンプト設定
            ExpanderInnerText1.Text = positivePrompt;
            ExpanderInnerText2.Text = negativePrompt;
            ExpanderInnerText3.Text = promptAll;
        }

        /// <summary>
        /// フォームロード
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // 展開コントロール用辞書
            Expanders.Add(Expander1);
            Expanders.Add(Expander2);
            Expanders.Add(Expander3);
            ExpanderHeightDic.Add(Expander1, ExpanderInner1);
            ExpanderHeightDic.Add(Expander2, ExpanderInner2);
            ExpanderHeightDic.Add(Expander3, ExpanderInner3);
            ExpanderDic.Add(Expander1, ExpanderInnerText1);
            ExpanderDic.Add(Expander2, ExpanderInnerText2);
            ExpanderDic.Add(Expander3, ExpanderInnerText3);

            // 展開後のコンテンツサイズ計算用
            ExpanderUnitHeight = Expander1.ActualHeight;
            InnerHeight = ParentGrid.ActualHeight - ExpanderUnitHeight * Expanders.Count;
            if (InnerHeight < 0) InnerHeight = 0;

            // 先頭を展開
            Expander1.IsExpanded = true;
        }

        /// <summary>
        /// コントロールサイズ変更
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            foreach (Expander expander in Expanders)
                if (expander.IsExpanded == false) { ExpanderUnitHeight = expander.ActualHeight; break; }

            InnerHeight = ParentGrid.ActualHeight - ExpanderUnitHeight * Expanders.Count;
            if (InnerHeight < 0) InnerHeight = 0;

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
        /// PositivePromptコピー
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_Tag1(object sender, RoutedEventArgs e)
        {
            System.Windows.Clipboard.SetText(ExpanderInnerText1.Text);
        }

        /// <summary>
        /// NegativePromptコピー
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_Tag2(object sender, RoutedEventArgs e)
        {
            System.Windows.Clipboard.SetText(ExpanderInnerText2.Text);
        }

        /// <summary>
        /// AllInfoコピー
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_Tag3(object sender, RoutedEventArgs e)
        {
            System.Windows.Clipboard.SetText(ExpanderInnerText3.Text);
        }

        /// <summary>
        /// 内容物のサイズ変更
        /// </summary>
        private void ResizeInner()
        {
            foreach (Expander expander in Expanders)
            {
                if (expander.IsExpanded) ExpanderHeightDic[expander].Height = InnerHeight;
                else ExpanderHeightDic[expander].Height = 0;
            }
        }

    }
}
