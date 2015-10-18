using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Windows.Media;
using Binginator.Classes;
using Binginator.Events;
using Binginator.Models;
using Microsoft.Win32.TaskScheduler;

namespace Binginator.Windows.ViewModels {
    public class MainViewModel {
        private MainModel _model;
        public event EventHandler<LogUpdatedEventArgs> LogUpdated;
        public uint ScheduleStart { get; set; }
        public uint ScheduleRandom { get; set; }


        public MainViewModel(MainModel model) {
            _model = model;
            _model.SetViewModel(this);

            ScheduleStart = 13;
            ScheduleRandom = 2;

            if (App.Arguments.ContainsKey("search"))
                SearchCommand.Execute(null);
        }

        internal void Quit() {
            _model.Quit(true);
        }

        private RelayCommand _SearchCommand;
        public RelayCommand SearchCommand {
            get {
                return _SearchCommand ?? (
                    _SearchCommand = new RelayCommand(
                        () => {
                            //LogUpdate("SearchCommand", Colors.DarkSlateGray);

                            var bw = new BackgroundWorker();
                            bw.DoWork += (sender, e) => { _model.Search(); };
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
                            //LogUpdate("LaunchDesktopCommand", Colors.DarkSlateGray);

                            var bw = new BackgroundWorker();
                            bw.DoWork += (sender, e) => { _model.Launch(false); };
                            bw.RunWorkerAsync();
                        }
                    ));
            }
        }

        private RelayCommand _LaunchMobileCommand;
        public RelayCommand LaunchMobileCommand {
            get {
                return _LaunchMobileCommand ?? (
                    _LaunchMobileCommand = new RelayCommand(
                        () => {
                            //LogUpdate("LaunchMobileCommand", Colors.DarkSlateGray);

                            var bw = new BackgroundWorker();
                            bw.DoWork += (sender, e) => { _model.Launch(true); };
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
                        () => { return Directory.Exists(Path.Combine(App.Folder, "profile")); }
                    ));
            }
        }

        private RelayCommand _OpenFolderCommand;
        public RelayCommand OpenFolderCommand {
            get {
                return _OpenFolderCommand ?? (
                    _OpenFolderCommand = new RelayCommand(
                        () => {
                            //LogUpdate("OpenFolderCommand", Colors.DarkSlateGray);

                            Process.Start(new ProcessStartInfo {
                                FileName = App.Folder,
                                UseShellExecute = true,
                                Verb = "open"
                            });
                        }
                    ));
            }
        }

        private bool? _TaskSchedulerChecked;
        public bool TaskSchedulerChecked {
            get {
                if (_TaskSchedulerChecked == null) {
                    LogUpdate("checking for scheduled task", Colors.LightGray);
                    _TaskSchedulerChecked = false;

                    try {
                        using (TaskService ts = new TaskService()) {
                            bool v2 = ts.HighestSupportedVersion >= new Version(1, 2);

                            Task task = ts.FindTask("binginator_" + WindowsIdentity.GetCurrent().Name.Replace(@"\", "-"));
                            if (task != null && task.Definition.Triggers.Count == 1) {
                                DailyTrigger trigger = task.Definition.Triggers[0] as DailyTrigger;
                                if (trigger != null) {
                                    ScheduleStart = (uint)trigger.StartBoundary.Hour;

                                    if (v2)
                                        ScheduleRandom = (uint)trigger.RandomDelay.Hours;

                                    _TaskSchedulerChecked = true;
                                }
                            }
                        }
                    }
                    catch (Exception) { }
                }

                return (bool)_TaskSchedulerChecked;
            }
            set {
                _TaskSchedulerChecked = value;

                string userName = WindowsIdentity.GetCurrent().Name;
                string taskName = "binginator_" + userName.Replace(@"\", "-");

                try {
                    using (TaskService ts = new TaskService()) {
                        if (value == true) {
                            LogUpdate("add scheduled task", Colors.DarkSlateGray);

                            bool v2 = ts.HighestSupportedVersion >= new Version(1, 2);
                            TaskDefinition td = ts.NewTask();

                            td.RegistrationInfo.Author = "Binginator";
                            td.RegistrationInfo.Description = "Start Binginator as configured";

                            td.Principal.UserId = userName;
                            if (v2)
                                td.Principal.LogonType = TaskLogonType.InteractiveToken;

                            td.Settings.DisallowStartIfOnBatteries = false;
                            td.Settings.ExecutionTimeLimit = TimeSpan.Zero;

                            DailyTrigger trigger = new DailyTrigger();
                            trigger.StartBoundary = DateTime.Today + TimeSpan.FromHours(ScheduleStart);
                            trigger.DaysInterval = 1;
                            if (v2)
                                trigger.RandomDelay = TimeSpan.FromHours(ScheduleRandom);

                            td.Triggers.Add(trigger);

                            td.Actions.Add(new ExecAction(
                                    '"' + Path.Combine(App.Folder, AppDomain.CurrentDomain.FriendlyName) + '"',
                                    "--search",
                                    App.Folder
                                ));

                            ts.RootFolder.RegisterTaskDefinition(taskName, td);
                        }
                        else {
                            LogUpdate("remove scheduled task", Colors.DarkSlateGray);
                            ts.RootFolder.DeleteTask(taskName);
                        }
                    }
                }
                catch (Exception) { }
            }
        }

        public void LogUpdate(string data, Color color, bool inline = false) {
            data = String.Format("{0} {1}", DateTime.Now.ToString("HH:mm:ss.fff"), data);

            if (LogUpdated != null)
                App.InvokeIfRequired(() => LogUpdated(this, new LogUpdatedEventArgs(data, color, inline)));
            else
                App.WriteLog(data + Environment.NewLine);
        }
    }
}
