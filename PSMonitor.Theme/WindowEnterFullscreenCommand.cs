using System;
using System.Windows.Input;

namespace PSMonitor.Theme
{
    public class WindowEnterFullscreenCommand : ICommand
    {

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter)
        {

            var window = parameter as Window;
            window.IsFullscreen = true;

        }
    }
}
