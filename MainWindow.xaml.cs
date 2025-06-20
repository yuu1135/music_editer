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
        private MediaPlayer player = new();
        private string music_file_path = null;
        private DispatcherTimer renderTimer = new();
        private DateTime startTime;
        private bool isPlaying = false;
        private double currentMs;

        private double BPM => double.TryParse(BpmTextBox.Text, out var bpm) ? bpm : 120;
        private const double PixelsPerBeat = 100;
        private const int LaneWidth = 100;


        public MainWindow() {
            InitializeComponent();

            renderTimer.Interval = TimeSpan.FromMilliseconds(16);
            renderTimer.Tick += RenderTimer_Tick;
            KeyDown += MainWindow_KeyDown;
            BpmTextBox.KeyDown += Bpmbox_KeyDown;

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

        private void Bpmbox_KeyDown(object sender, KeyEventArgs e) {
            if (!e.IsRepeat & Keyboard.GetKeyStates(Key.Enter).HasFlag(KeyStates.Down)) {
                DrawNotes();
            }

        }

            private void MainWindow_KeyDown(object sender, KeyEventArgs e) {
            if(!e.IsRepeat & Keyboard.GetKeyStates(Key.Space).HasFlag(KeyStates.Down)) {
                if(isPlaying) {
                    Music_Stop();
                }else {
                    Music_Play();
                }
            }
        }

        private void LoadMusic_Click(object sender, RoutedEventArgs e) {
            var dlg = new OpenFileDialog { Filter = "音楽ファイル|*.mp3;*.wav" };
            if (dlg.ShowDialog() == true) {
                music_file_path = dlg.FileName;
                player.Open(new Uri(music_file_path));
                player.MediaOpened += (s, _) => {
                    var duration = player.NaturalDuration.TimeSpan.TotalMilliseconds;
                    double canvasHeight = TimeToY(duration);
                    NoteCanvas.Height = Math.Max(canvasHeight, 1000); // 最低1000px
                    DrawNotes();
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
            player.Pause();
            renderTimer.Stop();
            isPlaying = false;


            double y = TimeToY(currentMs);
            CanvasScrollViewer.ScrollToVerticalOffset(y - CanvasScrollViewer.ViewportHeight / 2);

            DrawNotes();
        }


        private void Play_Click(object sender, RoutedEventArgs e) {
            Music_Play();
        }

        private void Stop_Click(object sender, RoutedEventArgs e) {
            Music_Stop();
        }

        private void Save_Click(object sender, RoutedEventArgs e) {
            var dlg = new SaveFileDialog { Filter = "譜面データ|*.msd" };
            if (dlg.ShowDialog() == true) {
                try {
                    MyFileIO.SaveToMyFile(Notes, BPM, music_file_path, dlg.FileName);
                    MessageBox.Show("保存しました", Title, MessageBoxButton.OK, MessageBoxImage.Information);
                }catch(Exception ex) {

                }
            }
        }

        private void Load_Click(object sender, RoutedEventArgs e) {
            var dlg = new OpenFileDialog { Filter = "譜面データ|*.msd" };
            if (dlg.ShowDialog() == true) {
                double bpm;
                try {
                    MyFileIO.LoadFromZipToString(dlg.FileName, out Notes, out bpm, out music_file_path);
                    BpmTextBox.Text = bpm.ToString();
                    player.Open(new Uri(music_file_path));
                    player.MediaOpened += (s, _) => {
                        var duration = player.NaturalDuration.TimeSpan.TotalMilliseconds;
                        double canvasHeight = TimeToY(duration);
                        NoteCanvas.Height = Math.Max(canvasHeight, 1000); // 最低1000px
                        DrawNotes();
                    };
                } catch(Exception ex) {

                }
            }
        }

        private void NoteCanvas_MouseDown(object sender, MouseButtonEventArgs e) {
            var pos = e.GetPosition(NoteCanvas);
            int lane = (int)(pos.X / LaneWidth);
            if (lane < 0 || lane > 3) return;

            double snappedY = Math.Round(pos.Y / 25.0) * 25.0;
            double time = YToTime(snappedY);

            if (e.ChangedButton == MouseButton.Left) {
                if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)) {
                    // Shiftキーを押しながら左クリックでその位置から再生
                    player.Position = TimeSpan.FromMilliseconds(time);
                    startTime = DateTime.Now - TimeSpan.FromMilliseconds(time);
                    player.Play();
                    isPlaying = true;
                    renderTimer.Start();
                } else {
                    Notes.Add(new Note { Lane = lane, Time = time });
                }
            } else if (e.ChangedButton == MouseButton.Right) {
                Notes.RemoveAll(n =>
                    n.Lane == lane &&
                    Math.Abs(TimeToY(n.Time) - pos.Y) < 10);
            }

            DrawNotes();
        }

        // 描写用マイマー
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

            // 時間目盛り（0ms から譜面の最後まで）
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
                        Text = $"{(int)(beat * 60_000 / BPM)}ms",
                        Foreground = Brushes.LightGray,
                        FontSize = 10
                    };
                    Canvas.SetLeft(text, 405);
                    Canvas.SetTop(text, y);
                    NoteCanvas.Children.Add(text);
                }
            }

            // ノート描画
            foreach (var note in Notes) {
                double x = note.Lane * LaneWidth;
                double y = TimeToY(note.Time);

                var rect = new Rectangle {
                    Width = 80,
                    Height = 10,
                    Fill = Brushes.Red,
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
            string processPath = Process.GetCurrentProcess().MainModule.FileName;

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
