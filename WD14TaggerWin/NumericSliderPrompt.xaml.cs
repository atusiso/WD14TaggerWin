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
    /// NumericSliderPrompt.xaml の相互作用ロジック
    /// </summary>
    public partial class NumericSliderPrompt : UserControl
    {
        /// <summary>精度スライダー編集制御フラグ</summary>
        private bool IsaccuracyChane = false;

        /// <summary>
        /// 値変更イベント
        /// </summary>
        public event EventHandler? ValueChanged;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public NumericSliderPrompt()
        {
            InitializeComponent();
        }

        /// <summary>タイトル</summary>
        public string Text
        {
            get
            {
                return (string?)(sliderName?.Content) ?? string.Empty;
            }
            set
            {
                if (sliderName != null) sliderName.Content = value;
            }
        }

        /// <summary>値</summary>
        public double Value
        {
            get
            {
                return accuracySlider?.Value ?? 0;
            }
            set
            {
                if (accuracySlider != null)
                    accuracySlider.Value = value;
            }
        }

        /// <summary>最小値</summary>
        public double Minimum
        {
            get
            {
                return accuracySlider?.Minimum ?? 0;
            }
            set
            {
                if (accuracySlider != null)
                    accuracySlider.Minimum = value;
            }
        }

        /// <summary>最大値</summary>
        public double Maximum
        {
            get
            {
                return accuracySlider?.Maximum ?? 0;
            }
            set
            {
                if (accuracySlider != null)
                    accuracySlider.Maximum = value;
            }
        }

        /// <summary>分解能</summary>
        public double TickFrequency
        {
            get
            {
                return accuracySlider?.TickFrequency ?? 0;
            }
            set
            {
                if (accuracySlider != null)
                    accuracySlider.TickFrequency = value;
            }
        }

        /// <summary>スキップ量</summary>
        public double LargeChange
        {
            get
            {
                return accuracySlider?.LargeChange ?? 0;
            }
            set
            {
                if (accuracySlider != null)
                    accuracySlider.LargeChange = value;
            }
        }

        /// <summary>変更量</summary>
        public double SmallChange
        {
            get
            {
                return accuracySlider?.SmallChange ?? 0;
            }
            set
            {
                if (accuracySlider != null)
                    accuracySlider.SmallChange = value;
            }
        }

        /// <summary>
        /// スライダー変更
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (IsaccuracyChane) return;
            IsaccuracyChane = true;

            // 結果をテキストに設定
            accuracyTextBox.Text = accuracySlider.Value.ToString("0.00");

            // 値変更を通知
            ValueChanged?.Invoke(this, new EventArgs());

            IsaccuracyChane = false;
        }

        /// <summary>
        /// テキスト変更
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (IsaccuracyChane) return;
            IsaccuracyChane = true;

            // double.Parseチェック
            double res = accuracySlider.Minimum;
            if (double.TryParse(accuracyTextBox.Text, out res))
            {
                // 正常な数値の場合
                if (res < accuracySlider.Minimum) res = accuracySlider.Minimum;
                if (res > accuracySlider.Maximum) res = accuracySlider.Maximum;
            }
            else
            {
                // 数値でない文字を入れた場合
                accuracyTextBox.Text = res.ToString("0.00");
            }
            // 結果をスライダーに設定
            accuracySlider.Value = res;

            // 値変更を通知
            ValueChanged?.Invoke(this, new EventArgs());

            IsaccuracyChane = false;
        }

        /// <summary>
        /// 精度テキストのフォーカス取得
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void accuracyTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            // フォーカスを得たら全選択
            accuracyTextBox.SelectAll();
        }

        /// <summary>
        /// 精度テキストのローカルマウス左クリック
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void accuracyTextBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // フォーカスを得てない場合はフォーカスを得てハンドル処理を修了
            if (accuracyTextBox.IsFocused) return;
            accuracyTextBox.Focus();
            e.Handled = true;
        }

        /// <summary>
        /// マウスホイール操作
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Threshold_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            float diff = (e.Delta / 120) * 0.01f;
            accuracySlider.Value += diff;
        }


    }
}
