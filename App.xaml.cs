using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
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
            if (e.Args.Contains("--register-url-protocol"))
            {
                if (RegisterUrlProtocol())
                    MessageBox.Show("登録に成功");

                else
                    MessageBox.Show("登録に失敗");

                Application.Current.Shutdown();
            }

            var mainWindow = new MainWindow();
            mainWindow.Show();
        }
    }
}
