using Microsoft.Win32;
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
    /// ConfigWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class ConfigWindow : Window
    {
        AppSettingXmlFile? ConfigData;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public ConfigWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// スケール変更
        /// </summary>
        /// <param name="scale"></param>
        public void InitScale(AppSettingXmlFile config, double scale)
        {
            Scale.ScaleX = scale;
            Scale.ScaleY = scale;

            MinWidth = MinWidth * scale;
            MinHeight = MinHeight * scale;
            Width = Width * scale;
            Height = Height * scale;

            ConfigData = config;
        }

        /// <summary>
        /// ウィンドウロード
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            WD14Path.Text = ConfigData?.CachePath;
            IsUnderbarToSpace.IsChecked = ConfigData?.IsUnderScoreToSpace;
            IsBranketsEscap.IsChecked = ConfigData?.IsTagBracketsEscape;
            IsAutoTagging.IsChecked = ConfigData?.IsDropToTagging;
            IsResultToClipboad.IsChecked = ConfigData?.IsReslutToClipbord;
            IsWinPosMemory.IsChecked = ConfigData?.IsWinPosMemory;
            IsMLDanbooruResizeNew.IsChecked = ConfigData?.IsMLDanbooruResizeNew;
            thrsholdSlider.Value = double.Parse(ConfigData?.Threshold2 ?? "0.8");
        }

        /// <summary>
        /// キャッシュパス選択ボタン押下
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFolderDialog openfolderDialog = new OpenFolderDialog();
            if (openfolderDialog.ShowDialog() == true)
            {
                WD14Path.Text = openfolderDialog.FolderName;
            }
        }

        /// <summary>
        /// Canselクリック
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            // 何もしないで修了
            this.Close();
        }

        /// <summary>
        /// OKクリック
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            if (ConfigData != null)
            {
                ConfigData.CachePath = WD14Path.Text;
                ConfigData.IsUnderScoreToSpace = (IsUnderbarToSpace.IsChecked ?? false);
                ConfigData.IsTagBracketsEscape = (IsBranketsEscap.IsChecked ?? false);
                ConfigData.IsDropToTagging = (IsAutoTagging.IsChecked ?? false);
                ConfigData.IsReslutToClipbord = (IsResultToClipboad.IsChecked ?? false);
                ConfigData.IsWinPosMemory = (IsWinPosMemory.IsChecked ?? false);
                ConfigData.IsMLDanbooruResizeNew = (IsMLDanbooruResizeNew.IsChecked ?? false);
                ConfigData.Threshold2 = thrsholdSlider.Value.ToString();

                ConfigData.UpdateToFile();
            }
            // 修了
            this.Close();
        }

    }
}
