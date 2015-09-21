using System;
using System.Windows.Input;

namespace Binginator.Classes {
    // based on http://mvvmlight.codeplex.com/
    public class RelayCommand : ICommand {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action execute) : this(execute, null) { }
        public RelayCommand(Action execute, Func<bool> canExecute) {
            if (execute == null)
                throw new ArgumentNullException("execute");

            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged {
            add {
                if (_canExecute != null)
                    CommandManager.RequerySuggested += value;
            }
            remove {
                if (_canExecute != null)
                    CommandManager.RequerySuggested -= value;
            }
        }

        public void RaiseCanExecuteChanged() {
            CommandManager.InvalidateRequerySuggested();
        }

        public bool CanExecute(object parameter) {
            return _canExecute == null || _canExecute();
        }

        public void Execute(object parameter) {
            if (_execute != null && CanExecute(parameter))
                _execute();
        }
    }
}
