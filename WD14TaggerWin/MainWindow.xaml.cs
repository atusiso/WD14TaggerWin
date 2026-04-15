using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics.Tensors;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CommonClass;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using WD14TaggerWin.ModelManager;

namespace WD14TaggerWin
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// <remarks>MVVMは犬に食わせた</remarks>
    public partial class MainWindow : Window
    {
        // --  --  --  --  --  --  --  --  --  --  --  --  --  --  --  --  --  --
        // 定数
        // --  --  --  --  --  --  --  --  --  --  --  --  --  --  --  --  --  --
        /// <summary>タグ表の最大表示数</summary>
        private static int MaxTagGraph = 1024;

        // --  --  --  --  --  --  --  --  --  --  --  --  --  --  --  --  --  --
        // レガシーメソッド(起動時のキー押下チェック用)
        // --  --  --  --  --  --  --  --  --  --  --  --  --  --  --  --  --  --
        [DllImport("user32.dll")]
        private static extern bool GetKeyboardState(byte[] lpKeyState);

        // --  --  --  --  --  --  --  --  --  --  --  --  --  --  --  --  --  --
        // 設定情報
        // --  --  --  --  --  --  --  --  --  --  --  --  --  --  --  --  --  --
        /// <summary>設定ファイル情報</summary>
        private AppSettingXmlFile ConfigFile = new AppSettingXmlFile();

        // --  --  --  --  --  --  --  --  --  --  --  --  --  --  --  --  --  --
        // Windowスケーリング関係
        // --  --  --  --  --  --  --  --  --  --  --  --  --  --  --  --  --  --
        /// <summary>スケーリング制御フラグ</summary>
        private bool IsResizeStart = false;
        /// <summary>スケーリング用基準幅(起動状態の値を基準にする)</summary>
        private double defaultWidth;
        /// <summary>スケーリング用基準高(起動状態の値を基準にする)</summary>
        private double defaultHeight;

        // --  --  --  --  --  --  --  --  --  --  --  --  --  --  --  --  --  --
        // 推論制御
        // --  --  --  --  --  --  --  --  --  --  --  --  --  --  --  --  --  --
        /// <summary>推論モデルマネージャ</summary>
        private ModelMan Interrogators = new ModelMan();
        /// <summary>推論対象画像パス</summary>
        private string TargetImagePath = string.Empty;
        /// <summary>推論対象画像実体</summary>
        private SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>? TagetImage = null;
        /// <summary>最終推論結果</summary>
        private ResultTagSet ResultTags = new ResultTagSet();

        // --  --  --  --  --  --  --  --  --  --  --  --  --  --  --  --  --  --
        // UI制御フラグ関係
        // --  --  --  --  --  --  --  --  --  --  --  --  --  --  --  --  --  --
        /// <summary>ウィンドウ起動位置初期化(起動時に左シフトを押し続けるか、-iオプションをつけて起動すると設定で覚えたWindow座標を無視)</summary>
        private bool IsWindowInit = false;
        /// <summary>精度スライダー編集制御フラグ</summary>
        private bool IsaccuracyChane = false;
        /// <summary>モデルダウンロード終了フラグ(非同期コールバックで修了判定)</summary>
        private bool DLComplete = true;
        /// <summary>閾値変更時の結果反映フラグ(画面反映のディレイ処理用)</summary>
        private bool IsResultUpdate = false;
        /// <summary>UIタイマー(ディレイ処理用0.5秒タイマー)</summary>
        private DispatcherTimer UITimer = new DispatcherTimer();

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

            // 起動ホットキーをチェック
            IsWindowInit = CheckStartHotkey();

            // 起動引数をチェック
            string[] args = Environment.GetCommandLineArgs();
            foreach(string arg in args)
            {
                if (arg == "-i") IsWindowInit = true;
            }
        }

        /// <summary>
        /// フォームロード
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // スケーリング変数の保持(起動時のサイズをベースとして保持)
            defaultWidth = Width;
            defaultHeight = Height;

            string productName = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyProductAttribute>()?.Product ?? "WD14TaggerWin";
            // 設定情報初期化
            try
            {
                ConfigFile = new AppSettingXmlFile(productName, null, null, true);
            }
            catch
            {
                MessageBox.Show("設定ファイルが壊れていたので初期化設定で起動します。", "警告", MessageBoxButton.OK, MessageBoxImage.Error);
                ConfigFile = new AppSettingXmlFile(productName, null, null, false);
                ConfigFile.UpdateToFile();
            }

            // ホットキーが押されている場合はウィンドウ位置の復元を行わない
            if (IsWindowInit) ConfigFile.IsWinPosMemory = false;
            // ウィンドウ位置の復元と表示
            if (ConfigFile.IsWinPosMemory) RestorationWindow();

            // 設定情報のチェックとタガー開始チェック
            InitState();

            // ウィンドウ状態の復元
            WindowState = WindowState.Normal;

            // スケーリング開始(スケール初期値の適用)
            DoScaling();
            IsResizeStart = true;

            // 処理中アニメーションを停止
            progressT.IsAnimate = false;

            // ディレイ表示用UIタイマー開始
            UITimer.Tick += Timer_Tick;
            UITimer.Interval = TimeSpan.FromMilliseconds(500);
            UITimer.Start();
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
            // 準備が整うまでのイベントは無視
            if (IsResizeStart == false) return;

            // スケーリング実施
            DoScaling();
        }

        /// <summary>
        /// タイマー処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Timer_Tick(object? sender, EventArgs e)
        {
            // 結果反映フラグがオンの場合
            if (IsResultUpdate)
            {
                // 表示結果を消去してタグを結果に反映
                ResultTags.InitViewResult();
                ResultTags.ApplyThreshold((float)accuracySlider.Value, ConfigFile.IsUnderScoreToSpace, ConfigFile.IsTagBracketsEscape);
                CreateResultTags();

                IsResultUpdate = false;
            }
        }

        /// <summary>
        /// タグ付けボタン押下
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_Interogate(object sender, RoutedEventArgs e)
        {
            // タグ付対象の画像がない場合
            if (TagetImage == null) return;

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
            if (IsaccuracyChane) return;
            IsaccuracyChane = true;

            // 結果をテキストに設定
            accuracyTextBox.Text = accuracySlider.Value.ToString("0.00");

            // タイマーで結果を画面に反映
            IsResultUpdate = true;

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

            // タイマーで結果を画面に反映
            IsResultUpdate = true;

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
            // モデルコンテンツの追加
            foreach (var model in Interrogators.interrogators)
            {
                liWindow.AddModelContent(model.Value.name, model.Value.author, model.Value.license, model.Value.siteuri);
            }
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
            if (TargetImagePath != string.Empty)openFileDialog.DefaultDirectory = TargetImagePath;
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
            CreateResultTags();
        }

        /// <summary>
        /// 推論結果コピー
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_Tag(object sender, RoutedEventArgs e)
        {
            System.Windows.Clipboard.SetText(resultTextBox.Text);
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
        /// ウィンドウのスケーリング実施
        /// </summary>
        private void DoScaling()
        {
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
                if (TagetImage != null)
                {
                    // 以前の画像を解放
                    TagetImage.Dispose();
                    TagetImage = null;
                }
                TagetImage = SixLabors.ImageSharp.Image.Load<SixLabors.ImageSharp.PixelFormats.Rgba32>(filePath);

                // 読み込み画像をImageに出力
                var stream = new System.IO.MemoryStream();
                TagetImage.SaveAsBmp(stream);
                stream.Seek(0, System.IO.SeekOrigin.Begin);
                BitmapSource bitmapSource =
                    System.Windows.Media.Imaging.BitmapFrame.Create(
                        stream,
                        System.Windows.Media.Imaging.BitmapCreateOptions.None,
                        System.Windows.Media.Imaging.BitmapCacheOption.OnLoad
                    );
                sourceImage.Source = bitmapSource;

                // 画像にInfoが無いかチェック
                (string, string, string, string) prompt = ImageTagCheck.CheckImageInfo(TagetImage, filePath);
                ImageInfo.SetContents(prompt.Item1, prompt.Item2, prompt.Item3, prompt.Item4);

                // 推論結果をクリア
                ClearTags();

                // 推論対象の画像設定と推論可能フラグをON
                TargetImagePath = filePath;

                if (ConfigFile.IsDropToTagging) DoTagging();
            }
            catch { }

        }

        /// <summary>
        /// 推論結果を消去
        /// </summary>
        private void ClearTags()
        {
            // 前の推論結果を削除
            ResultTags.InitResult();

            // カテゴリリスト消去
            categoryList.Items.Clear();
            categoryList.Items.Add(new ComboItemObject("すべて表示", "* All *"));
            categoryList.SelectedIndex = -1;

            // 結果テキストボックスをクリア
            resultTextBox.Text = string.Empty;

            // スタックパネルのグラフバーを解放
            foreach (var child in resultStackPanel.Children)
            {
                if (child is IDisposable disposableControl) disposableControl.Dispose();
            }
            resultStackPanel.Children.Clear();
        }

        /// <summary>
        /// 結果タグ画面の作成
        /// </summary>
        private void CreateResultTags()
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

                int tagNum = 0;
                foreach (string tag in ResultTags.GetCategoryTags(category))
                {
                    if (sb.Length != 0) sb.Append(", ");
                    sb.Append(tag);

                    // スタックパネルのグラフバーを生成
                    if ((ResultTags.resultTagDic.ContainsKey(tag)) && (tagNum < MaxTagGraph))
                    {
                        var tagLine = new TagLine(tag, ResultTags.resultTagDic[tag], 20);
                        resultStackPanel.Children.Add(tagLine);

                        // 表示が1024タグを超えたら無視
                        tagNum++;
                        if (tagNum == MaxTagGraph)
                        {
                            tagLine = new TagLine($"* Exceeded {MaxTagGraph.ToString()} tags *", 0.0f, 20);
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
                // モデルキャッシュ確認
                model.CheckCache(interrogator, ConfigFile.CachePath);

                // モデルがメモリに無くキャッシュファイルもない場合ダウンロードを実施してタガーを実施
                if ((model.IsModelLoad == false) && (model.IsCacheAvail == false)) DownloadModelTask(interrogator);
                // メモリにロードされているか、キャッシュファイルがある場合タガーを実施
                else DoTaggerTask();
            }
        }

        /// <summary>
        /// 非同期実行：モデルダウンロード実施
        /// </summary>
        /// <param name="key">モデルキー</param>
        private async void DownloadModelTask(string key)
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
            DoTaggerTask();
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
        private async void DoTaggerTask()
        {
            // 選択モデルのチェック(モデルが無い場合は中断)
            string interrogator = ((ComboItemObject)interrogatorList.SelectedItem).Value;
            AbstractTaggerModel? model = Interrogators[interrogator];
            if (model == null) return;

            // キャッシュが無い場合は中断
            if (model.IsCacheAvail == false) return;

            // 処理画像がない場合は中断
            if (TagetImage == null) return;

            // 閾値準備
            float threthold = float.Parse(ConfigFile.Threshold);

            // プログレスを表示に
            progressT.Visibility = Visibility.Visible;
            progressT.IsAnimate = true;

            // 推論の実行(非同期タスク)
            await Task.Run(() =>
            {
                // 推論の実施と結果の格納
                ResultTags.SetResult(model.interrogate(TagetImage, ConfigFile.IsMLDanbooruResizeNew));

                // 閾値を推論結果に適用
                ResultTags.InitViewResult();
                ResultTags.ApplyThreshold(threthold, ConfigFile.IsUnderScoreToSpace, ConfigFile.IsTagBracketsEscape);
            });

            // プログレスを非表示に
            progressT.Visibility = Visibility.Hidden;
            progressT.IsAnimate = false;

            // カテゴリリストを追加
            foreach (var category in ResultTags.GetCategories())
            {
                if (category != "* All *") categoryList.Items.Add(new ComboItemObject(category, category));
            }
            categoryList.SelectedIndex = 0;
        }

        /// <summary>
        /// 起動時ホットキーチェック
        /// </summary>
        /// <returns>true...ホットキーON/false...ホットキーOFF</returns>
        private bool CheckStartHotkey()
        {
            // 左のShiftを押しているかチェック
            byte[] keyStates = new byte[256];
            if (!GetKeyboardState(keyStates)) return false;
            return ((keyStates[KeyInterop.VirtualKeyFromKey(System.Windows.Input.Key.LeftShift)] & 0x80) != 0);
        }

    }
}