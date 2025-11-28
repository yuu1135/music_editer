using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Timers;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows.Media.Animation;

using music_editer.Models;
using music_editer.Utils;
using System.Windows.Input;
using System.Threading;
using System.Diagnostics;

namespace music_editer {
    public partial class MainWindow : Window {
        private List<Note> Notes = new();
        private MediaPlayer player;
        private string? music_file_path = null;
        private DispatcherTimer renderTimer = new();
        private DateTime startTime;
        private bool isPlaying = false;
        private double currentMs;

        private double BPM => double.TryParse(BpmTextBox.Text, out var bpm) ? bpm : 120;
        private bool bpmSet = false;    // BPM設定済みかどうか
        private const double PixelsPerBeat = 100;
        private const int LaneWidth = 120;
        
        public MainWindow() {
            InitializeComponent();

            renderTimer.Interval = TimeSpan.FromMilliseconds(16);
            renderTimer.Tick += RenderTimer_Tick;
            KeyDown += MainWindow_KeyDown;
            //BpmTextBox.KeyDown += Bpmbox_KeyDown;

            CompositionTarget.Rendering += (s, e) => {
                if (isPlaying) {
                    double currentTime = player.Position.TotalMilliseconds;
                    double y = TimeToY(currentTime); // Y時間読み出し

                    // スクロールに合わせて移動
                    //CanvasScrollViewer.ScrollToVerticalOffset(y - CanvasScrollViewer.ViewportHeight / 2);
                    //DrawNotes(CanvasScrollViewer.VerticalOffset);
                }
            };
        }

        /*
        private void Bpmbox_KeyDown(object sender, KeyEventArgs e) {
            if (!e.IsRepeat & Keyboard.GetKeyStates(Key.Enter).HasFlag(KeyStates.Down)) {
                //DrawNotes();
                if (player.NaturalDuration.HasTimeSpan) {
                    // BPMが変更された時の処理（再描画）
                    var duration = player.NaturalDuration.TimeSpan.TotalMilliseconds;
                    double canvasHeight = TimeToY(duration);
                    NoteCanvas.Height = Math.Max(canvasHeight, 1000);
                    DrawNotes();
                }
            }
        }
        */

        private void MainWindow_KeyDown(object sender, KeyEventArgs e) {
            if(!e.IsRepeat & Keyboard.GetKeyStates(Key.Space).HasFlag(KeyStates.Down)) {
                if(isPlaying) {
                    Music_Stop();
                }else {
                    Music_Play();
                }
            }
        }

        // 「曲読込」ボタン押下時
        private void LoadMusic_Click(object sender, RoutedEventArgs e) {
            var dlg = new OpenFileDialog { Filter = "音楽ファイル|*.mp3;*.wav" };
            if (dlg.ShowDialog() == true) {
                // BPMが未設定ならポップアップ表示し設定を促す
                if (!bpmSet)
                {
                    bpmSetting bpmWindow = new bpmSetting();
                    bpmWindow.Owner = this;
                    bool? result = bpmWindow.ShowDialog();
                    if (result == true && bpmWindow.BpmValue.HasValue)
                    {
                        BpmTextBox.Text = bpmWindow.BpmValue.Value.ToString();
                        bpmSet = true;
                        BpmTextBox.IsReadOnly = true;   // 一度設定したら編集禁止
                    }
                    else
                    {
                        MessageBox.Show("BPMが設定されなかったため、曲読込をキャンセルします。", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                        player.Close();
                        music_file_path = null;
                        return;
                    }
                }

                // 以下コードはセット
                music_file_path = dlg.FileName;

                //すでに開いている音楽ファイルを解放
                if (player != null)
                {
                    player.Close();
                }

                player = new();
                player.Open(new Uri(music_file_path));
                player.MediaOpened += (s, _) => {
                    if (player.NaturalDuration.HasTimeSpan) {
                        var duration = player.NaturalDuration.TimeSpan.TotalMilliseconds;
                        double canvasHeight = TimeToY(duration);
                        NoteCanvas.Height = Math.Max(canvasHeight, 1000);

                        Notes = new();

                        DrawNotes();
                    }
                };
            }
        }

        private void Music_Play() {
            if (player.Source == null) return;

            player.Play();
            isPlaying = true;
            renderTimer.Start();
        }

        private void Music_Stop() {
            // 音楽が読み込まれていない状態では描画しない
            if (player.Source == null || !player.NaturalDuration.HasTimeSpan)
                return;

            player.Pause();
            renderTimer.Stop();
            isPlaying = false;

            double y = TimeToY(currentMs);
            CanvasScrollViewer.ScrollToVerticalOffset(y - CanvasScrollViewer.ViewportHeight / 2);

            DrawNotes();
        }

        // 「曲再生」ボタン押下時
        private void Play_Click(object sender, RoutedEventArgs e) {
            Music_Play();
        }

        // 「曲停止」ボタン押下時
        private void Stop_Click(object sender, RoutedEventArgs e) {
            Music_Stop();
        }

        // 「譜面保存」ボタン押下時
        private void Save_Click(object sender, RoutedEventArgs e) {
            var dlg = new SaveFileDialog { Filter = "譜面データ|*.msd" };
            if (dlg.ShowDialog() == true) {
                try {
                    MyFileIO.SaveToMyFile(Notes, BPM, music_file_path, dlg.FileName);
                    MessageBox.Show("保存しました", Title, MessageBoxButton.OK, MessageBoxImage.Information);
                }catch (Exception)
                {

                }
            }
        }

        // 「譜面データ読込」ボタン押下時
        private void Load_Click(object sender, RoutedEventArgs e) {
            var dlg = new OpenFileDialog { Filter = "譜面データ|*.msd" };
            if (dlg.ShowDialog() == true) {
                double bpm;
                try {
                    MyFileIO.LoadFromZipToString(dlg.FileName, out Notes, out bpm, out music_file_path);
                    BpmTextBox.Text = bpm.ToString();
                    if (!string.IsNullOrEmpty(music_file_path)) {
                        //すでに開いている音楽ファイルを解放
                        if (player != null)
                        {
                            player.Close();
                        }

                        player = new();
                        player.Open(new Uri(music_file_path));
                        player.MediaOpened += (s, _) => {
                            if (player.NaturalDuration.HasTimeSpan) {
                                var duration = player.NaturalDuration.TimeSpan.TotalMilliseconds;
                                double canvasHeight = TimeToY(duration);
                                NoteCanvas.Height = Math.Max(canvasHeight, 1000);
                                DrawNotes();
                            }
                        };
                    }
                } catch (Exception) { }
            }
        }
        
        // ノーツ編集の動作等
        private void NoteCanvas_MouseDown(object sender, MouseButtonEventArgs e) {
            // 音楽が読み込まれていない状態では描画しない
            if (player.Source == null || !player.NaturalDuration.HasTimeSpan) {
                MessageBox.Show("音楽を読み込んでから操作してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var pos = e.GetPosition(NoteCanvas);
            int lane = (int)(pos.X / LaneWidth);
            if (lane < 0 || lane > 3) return;

            double snappedY = Math.Round(pos.Y / 25.0) * 25.0;
            double time = YToTime(snappedY);

            if (e.ChangedButton == MouseButton.Left) {          // マウス左クリックの動作
                if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                {
                    // Shiftキーで再生開始
                    player.Position = TimeSpan.FromMilliseconds(time);
                    startTime = DateTime.Now - TimeSpan.FromMilliseconds(time);
                    player.Play();
                    isPlaying = true;
                    renderTimer.Start();
                }
                else
                {
                    // ”同じ「時間」にすでにノーツがあるか”の判定
                    bool existsSameTime = Notes.Exists(n => Math.Abs(n.Time - time) < 1); // 1ms以内を同じとみなす
                    // 同じ時間にノーツがなければ新しくノーツを配置できる
                    if (!existsSameTime)
                    {
                        Notes.Add(new Note { Lane = lane, Time = time });
                    }
                    // すでにノーツがあったらそのノーツを削除し、新しくノーツを配置する
                    else
                    {
                        Notes.RemoveAll(n => Math.Abs(n.Time - time) < 1);
                        Notes.Add(new Note { Lane = lane, Time = time });
                    }
                }
            } else if (e.ChangedButton == MouseButton.Right) {  // マウス右クリックの動作
                // ノーツ削除
                Notes.RemoveAll(n =>
                    n.Lane == lane &&
                    Math.Abs(TimeToY(n.Time) - pos.Y) < 10);
            }
            DrawNotes();
        }

        // 描写用タイマー
        private void RenderTimer_Tick(object? sender, EventArgs e) {
            if (player.NaturalDuration.HasTimeSpan == false) return;

            currentMs = player.Position.TotalMilliseconds;
            CurrentTimeText.Text = TimeSpan.FromMilliseconds(currentMs).ToString(@"mm\:ss");

            double y = TimeToY(currentMs);
            CanvasScrollViewer.ScrollToVerticalOffset(y - CanvasScrollViewer.ViewportHeight / 2);

            DrawNotes(); // offsetYは内部で取得
        }

        private double TimeToY(double timeMs) {
            double beat = (timeMs / 1000.0) * (BPM / 60.0); // ms → beat
            return beat * PixelsPerBeat;
        }

        private double YToTime(double y) {
            double beat = y / PixelsPerBeat;
            return (beat * 60.0 / BPM) * 1000.0; // beat → ms
        }

        private void DrawNotes() {
            NoteCanvas.Children.Clear();

            // 音楽が読み込まれていない状態では描画しない
            if (player.Source == null || !player.NaturalDuration.HasTimeSpan)
                return;

            // レーン線
            for (int i = 0; i < 4; i++) {
                var laneLine = new Rectangle {
                    Width = 1,
                    Height = NoteCanvas.Height,
                    Fill = Brushes.Gray
                };
                Canvas.SetLeft(laneLine, i * LaneWidth);
                Canvas.SetTop(laneLine, 0);
                NoteCanvas.Children.Add(laneLine);
            }

            // 時間目盛り・小節表示
            double beatHeight = PixelsPerBeat;
            double totalBeats = TimeToY(player.NaturalDuration.TimeSpan.TotalMilliseconds) / PixelsPerBeat;
            for (double beat = 0; beat <= totalBeats; beat++) {
                double y = beat * beatHeight;

                bool isMeasureLine = ((int)beat % 4 == 0);
                var line = new Line {
                    X1 = 0,
                    X2 = NoteCanvas.Width,
                    Y1 = y,
                    Y2 = y,
                    Stroke = isMeasureLine ? Brushes.DimGray : Brushes.DarkSlateGray,
                    StrokeThickness = isMeasureLine ? 2 : 1
                };
                NoteCanvas.Children.Add(line);

                if (isMeasureLine) {
                    var text = new TextBlock {
                        //Text = $"{(int)(beat * 60_000 / BPM)}ms",
                        Text = $"{(int)(beat / 4)}小節",          //小節表示
                        Foreground = Brushes.Black,
                        FontSize = 20
                    };
                    //ノーツ範囲外(右端外)に表示
                    Canvas.SetLeft(text, 485);
                    Canvas.SetTop(text, y);
                    if ((int)(beat / 4) != 0)
                        NoteCanvas.Children.Add(text);
                }
            }

            // ノーツ描画
            foreach (var note in Notes) {
                double x = note.Lane * LaneWidth;
                double y = TimeToY(note.Time);

                // レーンごとの色を設定
                Brush noteColor = note.Lane switch
                {
                    0 => Brushes.Red,
                    1 => Brushes.Blue,
                    2 => Brushes.Green,
                    3 => Brushes.Yellow,
                    _ => Brushes.Gray // 万一レーンが異常値だったときの保険
                };

                var rect = new Rectangle {
                    Width = 80,
                    Height = 12,
                    Fill = noteColor,
                    RadiusX = 3,
                    RadiusY = 3
                };
                Canvas.SetLeft(rect, x);
                Canvas.SetTop(rect, y);
                NoteCanvas.Children.Add(rect);
            }

            if (isPlaying) {
                DrawRedLine();
            }
        }

        private double YToBeat(double y) {
            return y / PixelsPerBeat;
        }

        private void DrawRedLine() {
            RedLineCanvas.Children.Clear();

            double canvasWidth = NoteCanvas.Width;
            double viewportHeight = CanvasScrollViewer.ViewportHeight;
            double verticalOffset = CanvasScrollViewer.VerticalOffset;

            RedLineCanvas.Width = canvasWidth;
            RedLineCanvas.Height = viewportHeight;

            // 現在の再生時間
            double currentTime = player.Position.TotalMilliseconds;
            double playY = TimeToY(currentTime);

            double centerY = verticalOffset + viewportHeight / 2;

            double redLineY;

            if (playY < centerY) {
                // 譜面の再生位置が中央より上
                redLineY = playY - verticalOffset;
            } else {
                // 赤線は画面中央に固定
                redLineY = viewportHeight / 2;
            }

            // 赤線が画面内
            if (redLineY < 0) redLineY = 0;
            if (redLineY > viewportHeight) redLineY = viewportHeight;

            var redLine = new Line {
                X1 = 0,
                X2 = canvasWidth,
                Y1 = redLineY,
                Y2 = redLineY,
                Stroke = Brushes.Red,
                StrokeThickness = 2
            };

            RedLineCanvas.Children.Add(redLine);
        }

        // URLスキームのハンドラ
        private void Scheme_Click(object sender, RoutedEventArgs e)
        {
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string processPath = Environment.ProcessPath;

            var psi = new ProcessStartInfo
            {
                FileName = processPath,
                Arguments = "--register-url-protocol",
                UseShellExecute = true,
                Verb = "runas" // 管理者権限で実行
            };

            try
            {
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show("管理者権限での起動に失敗しました。\n" + ex.Message);
            }
        }
    }
}
