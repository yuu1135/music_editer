using System;
using System.Windows;

namespace music_editer
{
    public partial class bpmSetting : Window
    {
        public double? BpmValue { get; private set; } = null;

        public bpmSetting()
        {
            InitializeComponent();
        }

        //設定ボタンを押した時の処理
        private void SetBpmButton_Click(object sender, RoutedEventArgs e)
        {
            BpmEnter();
        }

        //キーボードのEnterキーを押した時の処理
        private void BpmInputTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if(e.Key == System.Windows.Input.Key.Enter)
            {
                BpmEnter();
            }
        }

        //BPM確定関数
        private void BpmEnter()
        {
            if (double.TryParse(BpmInputTextBox.Text, out double bpm))
            {
                if (bpm > 0 && bpm <= 300)  // BPMの妥当範囲チェック（任意）
                {
                    BpmValue = bpm;
                    this.DialogResult = true;
                    this.Close();
                }
                else
                {
                    MessageBox.Show("BPMは1以上300以下の数値で入力してください。", "入力エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            else
            {
                MessageBox.Show("有効な数値を入力してください", "入力エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
