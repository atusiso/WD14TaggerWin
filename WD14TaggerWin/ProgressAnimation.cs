using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
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
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

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
    ///     <MyNamespace:ProgressAnimation/>
    ///
    /// </summary>
    public class ProgressAnimation : Control
    {
        /// <summary>UIタイマー</summary>
        private DispatcherTimer UITimer = new DispatcherTimer();

        /// <summary>現在のサイクル</summary>
        private int NowCycle = 0;

        /// <summary>
        /// スタティックコンストラクタ
        /// </summary>
        static ProgressAnimation()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ProgressAnimation), new FrameworkPropertyMetadata(typeof(ProgressAnimation)));
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public ProgressAnimation()
        {
            // これが無いとOnRenderが正しく動かない糞仕様のゴミ
            Background = Brushes.Transparent;

            UITimer.Tick += Timer_Tick;
            UITimer.Interval = TimeSpan.FromMilliseconds(100);
        }

        /// <summary>
        /// 前景色(Backgroundが使えないからForegroundも使わない方針で)
        /// </summary>
        public static readonly DependencyProperty ForeColorProperty = DependencyProperty.Register(
            "ForeColor",
            typeof(Color),
            typeof(ProgressAnimation),
            new FrameworkPropertyMetadata(Color.FromRgb(255, 255, 255), FrameworkPropertyMetadataOptions.AffectsRender)
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
        /// アニメーションフラグ
        /// </summary>
        public static readonly DependencyProperty IsAnimateProperty = DependencyProperty.Register(
            "IsAnimate",
            typeof(bool),
            typeof(ProgressAnimation),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender)
        );

        /// <summary>
        /// プロパティ実態
        /// </summary>
        public bool IsAnimate
        {
            get { return (bool)GetValue(IsAnimateProperty); }
            set { SetValue(IsAnimateProperty, value); ResetTimerSetting(); }
        }

        /// <summary>
        /// アニメーション1週の秒数
        /// </summary>
        public static readonly DependencyProperty AnimationCycleProperty = DependencyProperty.Register(
            "AnimationCycle",
            typeof(double),
            typeof(ProgressAnimation),
            new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsRender)
        );

        /// <summary>
        /// プロパティ実態
        /// </summary>
        public double AnimationCycle
        {
            get { return (double)GetValue(AnimationCycleProperty); }
            set { if (value < 1.0) value = 1.0; SetValue(AnimationCycleProperty, value); ResetTimerSetting();  }
        }

        /// <summary>
        /// アニメーション分割数
        /// </summary>
        public static readonly DependencyProperty AnimationCounProperty = DependencyProperty.Register(
            "AnimationCount",
            typeof(int),
            typeof(ProgressAnimation),
            new FrameworkPropertyMetadata(16, FrameworkPropertyMetadataOptions.AffectsRender)
        );

        /// <summary>
        /// プロパティ実態
        /// </summary>
        public int AnimationCount
        {
            get { return (int)GetValue(AnimationCounProperty); }
            set { if (value < 1) value = 1; if (value > 32) value = 32;  SetValue(AnimationCounProperty, value); ResetTimerSetting(); }
        }

        /// <summary>
        /// タイマー処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Timer_Tick(object? sender, EventArgs e)
        {
            NowCycle += 1;
            if (AnimationCount <= NowCycle) NowCycle = 0;
            InvalidateVisual();
        }

        /// <summary>
        /// 描画処理
        /// </summary>
        /// <param name="drawingContext"></param>
        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            // 背景描画
            var rect = new Rect(0, 0, ActualWidth, ActualHeight);
            drawingContext.DrawRectangle(Background, null, rect);

            // アルファ値準備
            double alphaDiv = (255.0 / AnimationCount);
            double alpha = 255.0 - (alphaDiv * NowCycle);

            // 描画中心
            double cx = this.ActualWidth / 2.0;
            double cy = this.ActualHeight / 2.0;

            // 描画同心円半径(短い辺の1/4の半径)
            double or = this.ActualWidth / 4.0;
            if (this.ActualWidth > this.ActualHeight) or = this.ActualHeight / 4.0;
            double addRad = 2.0 * 3.14159 / AnimationCount;

            // 描画円半径(同心円の円周をアニメーション数の4倍で割る)
            double r = (2.0 * or * 3.14159) / (4.0 * AnimationCount);

            // 描画開始
            double nowRad = 0.0;
            for (int i = 0; i < AnimationCount; i++)
            {
                // 描画位置決定
                double sx = cx + Math.Sin(nowRad) * or;
                double sy = cy + Math.Cos(nowRad) * or;

                var brush = new SolidColorBrush(Color.FromArgb((byte)alpha, ForeColor.R, ForeColor.G, ForeColor.B));
                drawingContext.DrawEllipse(brush, null, new Point(sx, sy), r, r);

                alpha = alpha - alphaDiv;
                if (alpha < 0.0) alpha += 255.0;
                nowRad += addRad;
            }
        }

        /// <summary>
        /// タイマーセッティングをリセット
        /// </summary>
        private void ResetTimerSetting()
        {
            NowCycle = 0;
            UITimer.Interval = TimeSpan.FromMilliseconds(AnimationCycle * 1000.0 / AnimationCount);

            if (IsAnimate) UITimer.Start();
            else UITimer.Stop();

            InvalidateVisual();
        }
    }
}
