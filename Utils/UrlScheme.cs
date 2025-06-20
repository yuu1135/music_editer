using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace music_editer.Utils
{
    internal class UrlScheme
    {
        public static bool RegisterUrlProtocol()
        {
            string protocolName = "MusicEditer";
            string processPath = Process.GetCurrentProcess().MainModule.FileName;

            try
            {
                using (RegistryKey rootKey = Registry.ClassesRoot.CreateSubKey(protocolName))
                {
                    if (rootKey == null) return false;

                    rootKey.SetValue("", "URL:MusicEditer Protocol");
                    rootKey.SetValue("URL Protocol", "");

                    rootKey.CreateSubKey("DefaultIcon");

                    using (RegistryKey commandKey = rootKey.CreateSubKey(@"shell\open\command"))
                    {
                        if (commandKey == null) return false;
                        commandKey.SetValue("", $"\"{processPath}\" \"%1\"");
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("エラー: " + ex.Message);
                return false;
            }
        }


        public static void StartProcessAsAdmin(string exePath, string arguments = "")
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = exePath,              // 起動したい実行ファイルのパス
                    Arguments = arguments,           // 必要なら引数も指定
                    UseShellExecute = true,          // Shellを使って起動
                    Verb = "runas"                   // 管理者権限で起動
                };

                Process.Start(psi);
            }
            catch (Exception ex)
            {
                // ユーザーが UAC をキャンセルした場合など
                MessageBox.Show($"プロセスの起動に失敗しました: {ex.Message}");
            }

        }

    }
}
