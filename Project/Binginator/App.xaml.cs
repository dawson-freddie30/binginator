using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using Binginator.Windows;

namespace Binginator {
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>

    // icon from https://www.iconfinder.com/icons/670456/bing_communication_media_question_search_social_icon#size=64
    public partial class App : Application {
        public static string Folder = AppDomain.CurrentDomain.BaseDirectory;

        private void _Startup(object sender, StartupEventArgs e) {
            if (!File.Exists(Path.Combine(App.Folder, "chromedriver.exe")))
                new MsgWindow("Unable to locate required files. Reinstall this program.").ShowDialog();
            else {
                string latest = null;
#if !DEBUG
                ShutdownMode = ShutdownMode.OnExplicitShutdown;
                MsgWindow msgWindow = new MsgWindow("Checking for an update...");
                msgWindow.Show();

                try {
                    latest = new System.Net.WebClient().DownloadString("https://raw.githubusercontent.com/dawson-freddie30/binginator/release/latest").TrimEnd(new char[] { '\r', '\n' });
                }
                catch (System.Net.WebException ex) {
                    new MsgWindow("Update checking failed." + Environment.NewLine + Environment.NewLine + ex.Message).ShowDialog();
                }

                msgWindow.Close();
#endif

                string current = "v" + new Version(FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVer‌​sion).ToString(2);
                if (latest != null && current != latest) {
                    Process.Start(new ProcessStartInfo("msiexec.exe", "/I https://github.com/dawson-freddie30/binginator/releases/download/" + latest + "/Binginator.msi LAUNCH=1"));
                    Shutdown();
                }
                else {
                    ShutdownMode = ShutdownMode.OnLastWindowClose;
                    AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(_AssemblyResolve);
                    new MainWindow().Show();
                }
            }
        }

        private Assembly _AssemblyResolve(Object sender, ResolveEventArgs args) {
            string name = args.Name.Substring(0, args.Name.IndexOf(',')) + @".dll";

            if (name == "Binginator.resources.dll")
                return null;

            Debug.WriteLine("attempt to find assembly: " + name);

            using (Stream resource = _getEmbedded(name)) {
                if (resource == null)
                    return null;

                byte[] read = new byte[(int)resource.Length];
                resource.Read(read, 0, (int)resource.Length);
                return Assembly.Load(read);
            }
        }

        private static Stream _getEmbedded(string name) {
            return Assembly.GetExecutingAssembly().GetManifestResourceStream("Binginator.Embedded." + name);
        }

        public static void InvokeIfRequired(Action action) {
            if (!Current.Dispatcher.CheckAccess())
                Current.Dispatcher.Invoke(action);
            else
                action();
        }
    }
}
