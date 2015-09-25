using System;
using System.ComponentModel;
using System.IO;
using System.Windows.Media;
using Binginator.Classes;
using Binginator.Events;
using Binginator.Models;

namespace Binginator.Windows.ViewModels {
    public class MainViewModel {
        private MainModel _model;
        public uint MobileSearches { get; set; }
        public uint DesktopSearches { get; set; }
        public event EventHandler<LogUpdatedEventArgs> LogUpdated;

        public MainViewModel(MainModel model) {
            _model = model;
            _model.SetViewModel(this);

            MobileSearches = 20;
            DesktopSearches = 30;
        }

        internal void Quit() {
            _model.Quit(true);
        }

        private RelayCommand _LaunchMobileCommand;
        public RelayCommand LaunchMobileCommand {
            get {
                return _LaunchMobileCommand ?? (
                    _LaunchMobileCommand = new RelayCommand(
                        () => {
                            LogUpdate("LaunchMobileCommand", Colors.DarkSlateGray);

                            var bw = new BackgroundWorker();
                            bw.DoWork += (sender, e) => { _model.Launch(true); };
                            bw.RunWorkerAsync();
                        }
                    ));
            }
        }

        private RelayCommand _LaunchDesktopCommand;
        public RelayCommand LaunchDesktopCommand {
            get {
                return _LaunchDesktopCommand ?? (
                    _LaunchDesktopCommand = new RelayCommand(
                        () => {
                            LogUpdate("LaunchDesktopCommand", Colors.DarkSlateGray);

                            var bw = new BackgroundWorker();
                            bw.DoWork += (sender, e) => { _model.Launch(false); };
                            bw.RunWorkerAsync();
                        }
                    ));
            }
        }

        private RelayCommand _SearchCommand;
        public RelayCommand SearchCommand {
            get {
                return _SearchCommand ?? (
                    _SearchCommand = new RelayCommand(
                        () => {
                            LogUpdate("SearchCommand", Colors.DarkSlateGray);

                            var bw = new BackgroundWorker();
                            bw.DoWork += (sender, e) => { _model.Search(); };
                            bw.RunWorkerAsync();
                        }
                    ));
            }
        }

        private RelayCommand _ResetProfileCommand;
        public RelayCommand ResetProfileCommand {
            get {
                return _ResetProfileCommand ?? (
                    _ResetProfileCommand = new RelayCommand(
                        () => {
                            LogUpdate("ResetProfileCommand", Colors.DarkSlateGray);

                            _model.ResetProfile();
                        },
                        () => { return Directory.Exists("profile"); }
                    ));
            }
        }

        public void LogUpdate(string data, Color color, bool inline = false) {
            if (LogUpdated != null)
                App.InvokeIfRequired(() => LogUpdated(this, new LogUpdatedEventArgs(data, color, inline)));
        }
    }
}
