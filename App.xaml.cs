using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Telerik.Windows.Controls;
using WpfApp.Core;
using WpfApp.ViewModels;

namespace WpfApp {
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application {
        public static IAuthenticationProvider AuthenticationProvider { get; private set; }
        public static IDataContextFactory DataContextFactory { get; private set; }

        protected override void OnStartup(StartupEventArgs e) {
            try {
                RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
                AuthenticationProvider = new AuthenticationProvider();
                DataContextFactory = new DataContextFactory();

                base.OnStartup(e);

                StyleManager.ApplicationTheme = new VisualStudio2013Theme();

                Window dummyWindow = new Window();

                try {
                    RunDatabase();
                    LoginViewModel loginModel = new LoginViewModel();
                    if (UIHelper.EditObjectWithoutDbContext(loginModel) && loginModel.IsAuthenticated) {
                        MainWindow mainWindow = new WpfApp.MainWindow();
                        mainWindow.Show();
                    }
                } finally {
                    dummyWindow.Close();
                }
            } catch (Exception ex) {
                UIHelper.Error(null, ex);
                Application.Current.Shutdown();
            }
        }

        private Task RunDatabase() {
            return Task.Run(() => {
                var procs = Process.GetProcessesByName("rdbserver");
                if (procs.Length == 0) {
                    string dir = AppDomain.CurrentDomain.BaseDirectory + "\\[RedDatabase30]\\";
                    string path = dir + "rdbserver.exe";
                    Process.Start(new ProcessStartInfo() {
                        WorkingDirectory = dir,
                        FileName = path,
                        Arguments = "-a"
                    });
                }
            });
        }
    }
}
