using Binginator.Windows;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;

namespace Binginator {
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application {
        public static string Folder = AppDomain.CurrentDomain.BaseDirectory;
        private void _Startup(object sender, StartupEventArgs e) {
            if (!File.Exists(Path.Combine(App.Folder, "chromedriver.exe")))
                new MsgWindow("Unable to locate required files. Reinstall this program.").ShowDialog();
            else {
                AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(_AssemblyResolve);
                new MainWindow().Show();
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
    }
}
