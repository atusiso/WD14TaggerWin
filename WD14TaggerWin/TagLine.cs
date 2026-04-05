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
    /// このカスタム コントロールを XAML ファイルで使用するには、手順 1a または 1b の後、手順 2 に従います。
    ///
    /// 手順 1a) 現在のプロジェクトに存在する XAML ファイルでこのカスタム コントロールを使用する場合
    /// この XmlNamespace 属性を使用場所であるマークアップ ファイルのルート要素に
    /// 追加します:
    ///
    ///     xmlns:MyNamespace="clr-namespace:WD14TaggerWin"
    ///
    ///
    /// 手順 1b) 異なるプロジェクトに存在する XAML ファイルでこのカスタム コントロールを使用する場合
    /// この XmlNamespace 属性を使用場所であるマークアップ ファイルのルート要素に
    /// 追加します:
    ///
    ///     xmlns:MyNamespace="clr-namespace:WD14TaggerWin;assembly=WD14TaggerWin"
    ///
    /// また、XAML ファイルのあるプロジェクトからこのプロジェクトへのプロジェクト参照を追加し、
    /// リビルドして、コンパイル エラーを防ぐ必要があります:
    ///
    ///     ソリューション エクスプローラーで対象のプロジェクトを右クリックし、
    ///     [参照の追加] の [プロジェクト] を選択してから、このプロジェクトを参照し、選択します。
    ///
    ///
    /// 手順 2)
    /// コントロールを XAML ファイルで使用します。
    ///
    ///     <MyNamespace:TagLine/>
    ///
    /// </summary>
    public class TagLine : Control
    {
        /// <summary>
        /// スタティックコンストラクタ
        /// </summary>
        static TagLine()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TagLine), new FrameworkPropertyMetadata(typeof(TagLine)));
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public TagLine()
        {
            // これが無いとOnRenderが正しく動かない糞仕様のゴミ
            Background = Brushes.Transparent;
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="tag">表示タグ</param>
        /// <param name="percent">表示パーセント</param>
        /// <param name="height">コントロールの高さ</param>
        public TagLine(string tag, float percent, double height)
        {
            // これが無いとOnRenderが正しく動かない糞仕様のゴミ
            Background = Brushes.Transparent;

            // コントロール設定
            Text = tag;
            Percent = percent;
            Height = height;
        }

        /// <summary>
        /// ボーダーサイズ
        /// </summary>
        public static readonly DependencyProperty BorderSizeProperty =
            DependencyProperty.Register(
                "BorderSize",
                typeof(int),
                typeof(TagLine),
                new FrameworkPropertyMetadata(1, FrameworkPropertyMetadataOptions.AffectsRender)
            );

        /// <summary>
        /// プロパティ実態
        /// </summary>
        public int BorderSize
        {
            get { return (int)GetValue(BorderSizeProperty); }
            set { SetValue(BorderSizeProperty, value); }
        }


        /// <summary>
        /// パーセント
        /// </summary>
        public static readonly DependencyProperty PercentProperty = DependencyProperty.Register(
            "Percent",
            typeof(float),
            typeof(TagLine),
            new FrameworkPropertyMetadata(0.0f, FrameworkPropertyMetadataOptions.AffectsRender)
        );

        /// <summary>
        /// プロパティ実態
        /// </summary>
        public float Percent
        {
            get { return (float)GetValue(PercentProperty); }
            set { SetValue(PercentProperty, value); }
        }

        /// <summary>
        /// 表示テキスト
        /// </summary>
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
            "Text",
            typeof(string),
            typeof(TagLine),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsRender)
        );

        /// <summary>
        /// プロパティ実態
        /// </summary>
        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        /// <summary>
        /// 前景色(Backgroundが使えないからForegroundも使わない方針で)
        /// </summary>
        public static readonly DependencyProperty ForeColorProperty = DependencyProperty.Register(
            "ForeColor",
            typeof(Color),
            typeof(TagLine),
            new FrameworkPropertyMetadata(Color.FromRgb(0, 0, 0), FrameworkPropertyMetadataOptions.AffectsRender)
        );

        /// <summary>
        /// プロパティ実態
        /// </summary>
        public Color ForeColor
        {
            get { return (Color)GetValue(ForeColorProperty); }
            set { SetValue(ForeColorProperty, value); }
        }

        /// <summary>
        /// 背景色(Backgroundが使えないので…)
        /// </summary>
        public static readonly DependencyProperty BackColorProperty = DependencyProperty.Register(
            "BackColor",
            typeof(Color),
            typeof(TagLine),
            new FrameworkPropertyMetadata(Color.FromRgb(255, 255, 255), FrameworkPropertyMetadataOptions.AffectsRender)
        );

        /// <summary>
        /// プロパティ実態
        /// </summary>
        private Color BackColor
        {
            get { return (Color)GetValue(BackColorProperty); }
            set { SetValue(BackColorProperty, value); }
        }

        /// <summary>
        /// 最小色
        /// </summary>
        public static readonly DependencyProperty MinColorProperty = DependencyProperty.Register(
            "MinColor",
            typeof(Color),
            typeof(TagLine),
            new FrameworkPropertyMetadata(Color.FromRgb(255, 192, 192), FrameworkPropertyMetadataOptions.AffectsRender)
        );

        /// <summary>
        /// プロパティ実態
        /// </summary>
        private Color MinColor
        {
            get { return (Color)GetValue(MinColorProperty); }
            set { SetValue(MinColorProperty, value); }
        }

        /// <summary>
        /// 最小色
        /// </summary>
        public static readonly DependencyProperty MaxColorProperty = DependencyProperty.Register(
            "MaxColor",
            typeof(Color),
            typeof(TagLine),
            new FrameworkPropertyMetadata(Color.FromRgb(255, 32, 32), FrameworkPropertyMetadataOptions.AffectsRender)
        );

        /// <summary>
        /// プロパティ実態
        /// </summary>
        private Color MaxColor
        {
            get { return (Color)GetValue(MaxColorProperty); }
            set { SetValue(MaxColorProperty, value); }
        }

        /// <summary>
        /// 描画処理
        /// </summary>
        /// <param name="drawingContext"></param>
        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            double percent = Percent;
            if (percent < 0) percent = 0;
            if (percent > 1) percent = 1;

            var width = this.ActualWidth;
            var widthP = this.ActualWidth * percent;
            var height = this.ActualHeight;
            var borderSize = this.BorderSize;

            var backBrush = new SolidColorBrush(BackColor);
            var foreBrush = new SolidColorBrush(ForeColor);

            // 描画領域決定
            var rect = new Rect(borderSize, borderSize, width - borderSize * 2, height - borderSize * 2);
            //var rectP = new Rect(borderSize, borderSize, widthP - borderSize, height - borderSize * 2);

            // 背景描画
            drawingContext.DrawRectangle(backBrush, null, rect);

            // グラデーション描画 (割合描画)
            LinearGradientBrush gradientBrush = new LinearGradientBrush();
            gradientBrush.StartPoint = new Point(0, 0);
            gradientBrush.EndPoint = new Point(1, 0);
            gradientBrush.GradientStops.Add(new GradientStop(MinColor, 0.0));
            gradientBrush.GradientStops.Add(new GradientStop(MaxColor, 1.0));
            // var brush = new LinearGradientBrush(MinColor, MaxColor, new Point(borderSize, 0), new Point(width - borderSize * 2, 0));
            //var pen = new Pen(Foreground, 1);
            drawingContext.DrawRectangle(gradientBrush, null, rect);
            if (width - widthP - borderSize > 1.0)
            {
                var rectR = new Rect(widthP + borderSize, borderSize, width - widthP - borderSize, height - borderSize * 2);
                drawingContext.DrawRectangle(backBrush, null, rectR);
            }

            // テキスト描画
            double fontSize = 100;
            FormattedText formattedText = new FormattedText("  " + Text, System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Verdana"), fontSize, foreBrush, VisualTreeHelper.GetDpi(this).PixelsPerDip);
            while (formattedText.Height > (this.ActualHeight - borderSize * 2) && fontSize > 1)
            {
                fontSize -= 1;
                formattedText.SetFontSize(fontSize);
            }
            drawingContext.DrawText(formattedText, new Point(borderSize, borderSize));

            // パーセンテージ表示
            FormattedText formattedText2 = new FormattedText((percent * 100).ToString("0.0"), System.Globalization.CultureInfo.CurrentCulture, FlowDirection.RightToLeft, new Typeface("Verdana"), fontSize, foreBrush, VisualTreeHelper.GetDpi(this).PixelsPerDip);
            drawingContext.DrawText(formattedText2, new Point(width - borderSize * 2, borderSize));
        }

        /// <summary>
        /// コントトールのダブルクリック
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseDoubleClick(MouseButtonEventArgs e)
        {
            System.Windows.Clipboard.SetText(Text);
            base.OnMouseDoubleClick(e);
        }

    }
}
