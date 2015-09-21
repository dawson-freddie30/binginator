using System;
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
            _model.Quit();
        }

        private RelayCommand _LaunchMobileCommand;
        public RelayCommand LaunchMobileCommand {
            get {
                return _LaunchMobileCommand ?? (
                    _LaunchMobileCommand = new RelayCommand(
                        () => {
                            LogUpdate("LaunchMobileCommand", Colors.DarkSlateGray);

                            _model.Launch(true);
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

                            _model.Launch(false);
                        }
                    ));
            }
        }

        private RelayCommand _SearchCommand;
        public RelayCommand SearchCommand {
            get {
                return _SearchCommand ?? (
                    _SearchCommand = new RelayCommand(
                        async () => {
                            LogUpdate("SearchCommand", Colors.DarkSlateGray);

                            _model.Prepare();
                            try {
                                await _model.SearchMobile();
                                await _model.SearchDesktop();
                            }
                            catch (Exception ex) {
                                LogUpdate("WHAT THE HELL CHROMEDRIVER?! " + ex.Message, Colors.Red);
                            }
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
                LogUpdated(this, new LogUpdatedEventArgs(data, color, inline));
        }
    }
}
