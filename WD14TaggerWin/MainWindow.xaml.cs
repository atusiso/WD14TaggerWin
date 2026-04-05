using CommonClass;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics.Tensors;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using WD14TaggerWin.ModelManager;

namespace WD14TaggerWin
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// <remarks>MVVMは犬に食わせた</remarks>
    public partial class MainWindow : Window
    {
        /// <summary>スケーリング制御フラグ</summary>
        private bool isResizeStart = false;
        /// <summary>スケーリング用基準幅(起動状態の値を基準にする)</summary>
        private double defaultWidth;
        /// <summary>スケーリング用基準高(起動状態の値を基準にする)</summary>
        private double defaultHeight;
        /// <summary>精度スライダー編集制御フラグ</summary>
        private bool isaccuracyChane = false;

        /// <summary>タガー利用可否チェック</summary>
        private bool isTaggerValid = false;
        /// <summary>対象画像パス</summary>
        private string targetImagePath = string.Empty;
        /// <summary>対象画像</summary>
        private SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>? tagetImage = null;
        /// <summary>ダウンロード終了フラグ</summary>
        private bool DLComplete = true;

        /// <summary>結果カテゴリ辞書</summary>
        private Dictionary<string, List<string>> categoryDic = new();
        /// <summary>結果タグ辞書</summary>
        private Dictionary<string, float> resultTagDic = new();

        /// <summary>設定ファイル</summary>
        private AppSettingXmlFile ConfigFile = new AppSettingXmlFile();

        /// <summary>モデルマネージャ</summary>
        private ModelMan Interrogators = new ModelMan();

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            // ユニコード以外無視されているので
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            // ファイルのアセンブリバージョンをタイトルに表示
            var module = System.Diagnostics.Process.GetCurrentProcess().MainModule;
            if (module != null)
            {
                var ver = System.Diagnostics.FileVersionInfo.GetVersionInfo(module.FileName);
                this.Title += " " + ver.FileVersion;
            }
        }

        /// <summary>
        /// フォームロード
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // 設定情報初期化
            try
            {
                ConfigFile = new AppSettingXmlFile("WD14TaggerWin", null, null, true);
            }
            catch
            {
                MessageBox.Show("設定ファイルが壊れていたので初期化設定で起動します。", "警告", MessageBoxButton.OK, MessageBoxImage.Error);
                ConfigFile = new AppSettingXmlFile("WD14TaggerWin", null, null, false);
                ConfigFile.UpdateToFile();
            }

            // ウィンドウ位置の復元と表示
            if (ConfigFile.IsWinPosMemory) RestorationWindow();

            // 設定情報のチェックとタガー開始チェック
            InitState();

            // ウィンドウ状態の復元
            WindowState = WindowState.Normal;

            // スケーリング変数の保持
            defaultWidth = Width;
            defaultHeight = Height;
            isResizeStart = true;

            progressT.IsAnimate = false;
        }

        /// <summary>
        /// フォーム終了
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // 設定情報の保存
            string? interrogator = ((ComboItemObject)interrogatorList.SelectedItem)?.Value;
            string threshold = accuracyTextBox.Text;
            if (interrogator != null) ConfigFile.Interrogator = interrogator;
            ConfigFile.Threshold = threshold;
            ScreenToConfig();
            ConfigFile.UpdateToFile();
        }

        /// <summary>
        /// フォームサイズ変更
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (isResizeStart == false) return;

            // 縦スケールに基づく倍率設定(1～1.5スケーリングを行う)
            double scale = ActualHeight / defaultHeight;
            if (scale < 1) scale = 1;
            if (scale > 1.5) scale = 1.5;
            Scale.ScaleX = scale;
            Scale.ScaleY = scale;

            // 横サイズの最小限界
            MinWidth = 680 * scale;
            MinHeight = 480 * scale;
        }

        /// <summary>
        /// タグ付けボタン押下
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_Interogate(object sender, RoutedEventArgs e)
        {
            if (isTaggerValid == false) return;

            // 推論結果をクリア
            ClearTags();

            // 推論開始
            DoTagging();
        }

        /// <summary>
        /// スライダー変更
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (isaccuracyChane) return;
            isaccuracyChane = true;

            // 結果をテキストに設定
            accuracyTextBox.Text = accuracySlider.Value.ToString("0.00");

            isaccuracyChane = false;
        }

        /// <summary>
        /// テキスト変更
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (isaccuracyChane) return;
            isaccuracyChane = true;

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

            isaccuracyChane = false;
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

        /// <summary>
        /// 解放ボタン押下
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_Free(object sender, RoutedEventArgs e)
        {
            // 全てのモデルをメモリから解放
            Interrogators.FreeAllModels();
        }

        /// <summary>
        /// 設定ボタンクリック
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_Config(object sender, RoutedEventArgs e)
        {
            // 設定ダイアログ表示
            ConfigWindow cfWindow = new ConfigWindow();
            cfWindow.InitScale(ConfigFile, Scale.ScaleX);
            cfWindow.ShowDialog();

            // 設定情報のチェックとタガー開始チェック
            InitState();
        }

        /// <summary>
        /// 権利表示ボタンクリック
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_Copyright(object sender, RoutedEventArgs e)
        {
            // 権利表示ダイアログ表示
            LicenseWindow liWindow = new LicenseWindow();
            liWindow.InitScale(Scale.ScaleX);
            liWindow.ShowDialog();
        }

        /// <summary>
        /// ファイルのドラッグ判定
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_PreviewDragOver(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                var fileNames = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
                if (fileNames.Length == 1)
                {
                    e.Effects = System.Windows.DragDropEffects.Copy;
                    e.Handled = true;
                    return;
                }
            }
            e.Effects = System.Windows.DragDropEffects.None;
            e.Handled = true;
        }

        /// <summary>
        /// ファイルのドロップ実施
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_PreviewDrop(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                var fileNames = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
                if ((fileNames.Length == 1) && (interrogatorList.SelectedItem != null))
                {
                    // 一つのファイルの場合画像読み込み実施
                    ImageLoad(fileNames[0]);
                }
            }
        }

        /// <summary>
        /// イメージコントロールクリック
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Filter = "画像ファイル|*.png;*.jpg;*.jpeg;*.webp;*.bmp|全てのファイル|*.*";
            if (targetImagePath != string.Empty)openFileDialog.DefaultDirectory = targetImagePath;
            if (openFileDialog.ShowDialog() == true)
            {
                ImageLoad(openFileDialog.FileName);
            }
        }

        /// <summary>
        /// カテゴリ変更
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void categoryList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 結果タグ
            StringBuilder sb = new StringBuilder();

            // スタックパネルのグラフバーを解放
            foreach (var child in resultStackPanel.Children)
            {
                if (child is IDisposable disposableControl) disposableControl.Dispose();
            }
            resultStackPanel.Children.Clear();

            // 選択カテゴリに応じてカテゴリ辞書決定
            if (categoryList.SelectedItem != null)
            {
                ComboItemObject selCat = (ComboItemObject)categoryList.SelectedItem;
                string category = selCat.Value;

                if (categoryDic.ContainsKey(category))
                {
                    foreach (string tag in categoryDic[category])
                    {
                        if (sb.Length != 0) sb.Append(", ");
                        sb.Append(tag);

                        // スタックパネルのグラフバーを生成
                        if (resultTagDic.ContainsKey(tag))
                        {
                            var tagLine = new TagLine(tag, resultTagDic[tag], 20);
                            resultStackPanel.Children.Add(tagLine);
                        }
                    }
                }
            }

            // 結果タグ一覧を表示
            resultTextBox.Text = sb.ToString(); ;

            // 結果をクリップボードにコピー
            if (ConfigFile.IsReslutToClipbord) System.Windows.Clipboard.SetText(resultTextBox.Text);
        }

        /// <summary>
        /// ウィンドウ位置復元
        /// </summary>
        private void RestorationWindow()
        {
            // スクリーンがある場合
            PresentationSource source = PresentationSource.FromVisual(this);
            if (source != null)
            {
                // 設定に合わせて移動
                Top = ConfigFile.WindowTop;
                Left = ConfigFile.WindowLeft;
                // 設定に合わせてリサイズ
                Width = ConfigFile.WindowWidth;
                Height = ConfigFile.WindowHeight;

                // モニタ一覧を取得するのにSystem.Windows.Formsが必要ってマジ？wpfで完結してや…
                // ココから下のコードをコメン排除すると画面外にアプリが復元した時の判定と位置リセット機能が働く

                //// WPF論理座標をスクリーン座標に変換してリストに登録(DPIが異なる環境用)
                //System.Windows.Media.Matrix matrix = source.CompositionTarget.TransformToDevice;
                //var winbounds = new List<System.Drawing.Point>();
                //var leftTop = matrix.Transform(new System.Windows.Point(Left, Top));
                //winbounds.Add(new System.Drawing.Point((int)leftTop.X, (int)leftTop.Y));
                //var rightTop = matrix.Transform(new System.Windows.Point(Left + Width, Top));
                //winbounds.Add(new System.Drawing.Point((int)rightTop.X, (int)rightTop.Y));
                //var leftBottom = matrix.Transform(new System.Windows.Point(Left, Top + Height));
                //winbounds.Add(new System.Drawing.Point((int)leftTop.X, (int)leftTop.Y));
                //var rightBottom = matrix.Transform(new System.Windows.Point(Left + Width, Top + Height));
                //winbounds.Add(new System.Drawing.Point((int)rightTop.X, (int)rightTop.Y));

                //// 全モニタの範囲チェック
                //var screens = System.Windows.Forms.Screen.AllScreens;
                //int check = 0;
                //foreach (var screen in screens)
                //{
                //    // windowの四隅座標がスクリーンに入っていれば対応ビットを1にする
                //    int flag = 1;
                //    foreach (var pt in winbounds)
                //    {
                //        if (screen.Bounds.Contains(pt)) check = check | flag;
                //        flag = flag << 1;
                //    }
                //}

                //// 画面外チェック(bit0～3が全部立っていない/どこかが画面外にある)
                //if (check != 15)
                //{
                //    // アプリのサイズを初期値に戻す
                //    this.Height = 793;
                //    this.Width = 882;

                //    // 代表モニタがある場合
                //    System.Windows.Forms.Screen? pSc = System.Windows.Forms.Screen.PrimaryScreen;
                //    if (pSc != null)
                //    {
                //        int pLeft = pSc.Bounds.Left;
                //        int pTop = pSc.Bounds.Top;

                //        // メインモニタの左上座標に移動
                //        System.Windows.Media.Matrix matrix2 = source.CompositionTarget.TransformFromDevice;
                //        var scPt = matrix.Transform(new System.Windows.Point(pLeft, pTop));
                //        this.Top = scPt.X;
                //        this.Left = scPt.Y;
                //    }
                //    else
                //    {
                //        // 規定windowが無い場合は0,0に移動
                //        this.Top = 0;
                //        this.Left = 0;
                //    }
                //}
            }
        }

        /// <summary>
        /// 画面位置を設定情報に保持
        /// </summary>
        private void ScreenToConfig()
        {
            ConfigFile.WindowTop = Top;
            ConfigFile.WindowLeft = Left;
            ConfigFile.WindowWidth = Width;
            ConfigFile.WindowHeight = Height;
        }

        /// <summary>
        /// 初期化実施
        /// </summary>
        private void InitState()
        {
            // モデルダウンロードパスを設定
            Interrogators.ChangeCachePath(ConfigFile.CachePath);
            List<(string, string)> interrogators = Interrogators.Models();

            // インタロゲータリスト更新
            interrogatorList.Items.Clear();
            int index = 0;
            int selIndex = -1;
            while (index < interrogators.Count)
            {
                interrogatorList.Items.Add(new ComboItemObject(interrogators[index].Item2, interrogators[index].Item1));
                if (interrogators[index].Item1 == ConfigFile.Interrogator) selIndex = index;

                index++;
            }
            interrogatorList.SelectedIndex = selIndex;

            // 閾値設定
            accuracyTextBox.Text = ConfigFile.Threshold;
        }

        /// <summary>
        /// 画像ファイルのロードと推論準備
        /// </summary>
        /// <param name="filePath">画像ファイルパス</param>
        private void ImageLoad(string filePath)
        {

            try
            {
                // 対象イメージをロード
                if (tagetImage != null)
                {
                    // 以前の画像を解放
                    tagetImage.Dispose();
                    tagetImage = null;
                }
                tagetImage = SixLabors.ImageSharp.Image.Load<SixLabors.ImageSharp.PixelFormats.Rgba32>(filePath);

                // 読み込み画像をImageに出力
                var stream = new System.IO.MemoryStream();
                tagetImage.SaveAsBmp(stream);
                stream.Seek(0, System.IO.SeekOrigin.Begin);
                BitmapSource bitmapSource =
                    System.Windows.Media.Imaging.BitmapFrame.Create(
                        stream,
                        System.Windows.Media.Imaging.BitmapCreateOptions.None,
                        System.Windows.Media.Imaging.BitmapCacheOption.OnLoad
                    );
                sourceImage.Source = bitmapSource;

                // 画像にInfoが無いかチェック
                (string, string, string, string) prompt = ImageTagCheck.CheckImageInfo(tagetImage, filePath);
                ImageInfo.SetContents(prompt.Item1, prompt.Item2, prompt.Item3, prompt.Item4);

                // 推論結果をクリア
                ClearTags();

                // 推論対象の画像設定と推論可能フラグをON
                targetImagePath = filePath;
                isTaggerValid = true;

                if (ConfigFile.IsDropToTagging) DoTagging();
            }
            catch { }

        }

        /// <summary>
        /// 推論結果を消去
        /// </summary>
        private void ClearTags()
        {
            // カテゴリリスト消去
            categoryDic.Clear();
            categoryDic.Add("* All *", new List<string>());
            categoryList.Items.Clear();
            categoryList.Items.Add(new ComboItemObject("すべて表示", "* All *"));
            categoryList.SelectedIndex = -1;

            // 結果テキストボックスをクリア
            resultTagDic.Clear();
            resultTextBox.Text = string.Empty;

            // スタックパネルのグラフバーを解放
            foreach (var child in resultStackPanel.Children)
            {
                if (child is IDisposable disposableControl) disposableControl.Dispose();
            }
            resultStackPanel.Children.Clear();
        }

        /// <summary>
        /// タグ付け開始
        /// </summary>
        private void DoTagging()
        {
            // 選択interrogatorと閾値を取得
            string interrogator = ((ComboItemObject)interrogatorList.SelectedItem).Value;
            string threshold = accuracyTextBox.Text;
            ConfigFile.Interrogator = interrogator;
            ConfigFile.Threshold = threshold;
            // 画面位置を取得/設定ファイルを保存
            ScreenToConfig();
            ConfigFile.UpdateToFile();

            // 選択モデルを取得
            AbstractTaggerModel? model = Interrogators[interrogator];
            if (model != null)
            {
                // モデルがメモリに無くキャッシュファイルもない場合ダウンロードを実施してタガーを実施
                if ((model.IsModelLoad == false) && (model.IsCacheAvail == false)) DownloadModelTask(interrogator, tagetImage);
                // メモリにロードされているか、キャッシュファイルがある場合タガーを実施
                else DoTaggerTask(tagetImage);
            }
        }

        /// <summary>
        /// 非同期実行：モデルダウンロード実施
        /// </summary>
        /// <param name="key">モデルキー</param>
        private async void DownloadModelTask(string key, SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>? image)
        {
            if (Interrogators[key] == null) return;

            string dlTargetPath = ConfigFile.CachePath;

            // プログレスを表示に
            progressD.DlProgress.Value = 0.0;
            progressD.Visibility = Visibility.Visible;

            // ダウンロード開始(非同期タスク)
            DLComplete = false;
            Interrogators[key]?.Download(key, dlTargetPath, new AbstractTaggerModel.DLProgress(DLProgress));

            // ダウンロード終了待ち
            await Task.Run(() =>
            {
                // プログレスコールバックでフラグ設定を実施
                while (DLComplete == false) Thread.Sleep(100);

            });
            Interrogators[key]?.CheckCache(key, dlTargetPath);

            // プログレスを非表示に
            progressD.Visibility = Visibility.Collapsed;

            // 推論の実行を継続
            DoTaggerTask(image);
        }

        /// <summary>
        /// ダウンロード進捗表示(コールバック処理)
        /// </summary>
        /// <param name="percent">進捗割合</param>
        /// <param name="isComplete">完了フラグ</param>
        /// <param name="isFailed">失敗フラグ</param>
        private void DLProgress(double percent, bool isComplete, bool isFailed)
        {
            // 進捗率を画面に反映(別スレッドからメッセージポンプに積む)
            Dispatcher.BeginInvoke(new Action(() =>
            {
                // 完了か失敗でDL修了
                if (isComplete) DLComplete = true;
                if (isFailed) DLComplete = true;

                // プログレスバー設定
                progressD.DlProgress.Value = percent;
            }));
        }

        /// <summary>
        /// 非同期：推論の実行
        /// </summary>
        /// <param name="fileName"></param>
        private async void DoTaggerTask(SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>? image)
        {
            // 選択モデルのチェック(モデルが無い場合は中断)
            string interrogator = ((ComboItemObject)interrogatorList.SelectedItem).Value;
            AbstractTaggerModel? model = Interrogators[interrogator];
            if (model == null) return;

            // キャッシュが無い場合は中断
            if (model.IsCacheAvail == false) return;

            // 処理画像がない場合は中断
            if (image == null) return;

            // 閾値準備
            float threthold = float.Parse(ConfigFile.Threshold);

            // プログレスを表示に
            progressT.Visibility = Visibility.Visible;
            progressT.IsAnimate = true;

            // 推論の実行(非同期タスク)
            await Task.Run(() =>
            {
                var res = model.interrogate(image, ConfigFile.IsMLDanbooruResizeNew);

                // 結果タグ一覧から閾値順にソート
                var sortedDict = res.Item2.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
                foreach (var kvPair in sortedDict)
                {
                    // 閾値以上のタグのみに絞る
                    if (kvPair.Value > threthold)
                    {
                        // エスケープ処理
                        string tag = kvPair.Key;
                        if (ConfigFile.IsUnderScoreToSpace) tag = tag.Replace('_', ' ');
                        if (ConfigFile.IsTagBracketsEscape)
                        {
                            tag = tag.Replace("(", "\\(");
                            tag = tag.Replace(")", "\\)");
                        }
                        // 結果タグリストに登録
                        resultTagDic.Add(tag, kvPair.Value);

                        // カテゴリ辞書に登録
                        categoryDic["* All *"].Add(tag);
                        string category = (res.Item3.ContainsKey(tag) ? res.Item3[tag] : string.Empty);
                        if (category != string.Empty)
                        {
                            if (categoryDic.ContainsKey(category) == false)
                            {
                                categoryDic.Add(category, new List<string>());
                            }
                            categoryDic[category].Add(tag);
                        }
                    }
                }
            });

            // プログレスを非表示に
            progressT.Visibility = Visibility.Hidden;
            progressT.IsAnimate = false;

            // カテゴリリストを追加
            foreach (var category in categoryDic.Keys)
            {
                if (category != "* All *") categoryList.Items.Add(new ComboItemObject(category, category));
            }
            categoryList.SelectedIndex = 0;
        }

    }
}