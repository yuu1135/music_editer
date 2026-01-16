using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using static music_editer.Utils.UrlScheme;

namespace music_editer {
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            string? filePath;
            if (e.Args.Contains("--register-url-protocol"))
            {
                if (RegisterUrlProtocol())
                    MessageBox.Show("登録に成功");

                else
                    MessageBox.Show("登録に失敗");

                Application.Current.Shutdown();
            }else if(e.Args.Length > 0)
            {
                filePath = e.Args[0];
                if (File.Exists(filePath))
                {
                    var mainWindow = new MainWindow();
                    mainWindow.filePath = filePath;
                    mainWindow.Show();
                }
                else
                {
                    var mainWindow = new MainWindow();
                    mainWindow.Show();
                }
            }
            else
            {
                var mainWindow = new MainWindow();
                mainWindow.Show();
            }
        }
    }
}
