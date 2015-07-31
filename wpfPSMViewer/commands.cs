using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace PSMViewer
{

    public class RelayCommand : CommandParameter, ICommand
    {

        public RelayCommand() : base() { }

        public RelayCommand(Action<object, object> execute, Func<object, object, bool> canExecute, params object[] args) : base(args)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        private Action<object, object> _execute
        {
            get; set;
        } = null;

        private Func<object, object, bool> _canExecute
        {
            get; set;
        } = null;

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter)
        {

            if (_canExecute != null)
                return _canExecute(this, parameter);

            return parameter != null;

        }

        public void Execute(object parameter)
        {
            if (_execute != null)
                _execute(this, parameter);
        }

    }

    public class CommandArgumentsList : List<CommandArgument> {}

    public class CommandParameter : DependencyObject
    {

        public CommandParameter() {}

        public CommandParameter(params object[] args)
        {
            foreach (object arg in args)
            {
                this.Arguments.Add(new CommandArgument(arg));
            }
        }

        public CommandArgumentsList Arguments { get; set; } = new CommandArgumentsList();

    }

    public class CommandArgument : DependencyObject
    {
        public CommandArgument() { }

        public CommandArgument(object value)
        {
            this.Value = value;
        }

        public string Name
        {
            get { return (string)GetValue(NameProperty); }
            set { SetValue(NameProperty, value); }
        }

        public static readonly DependencyProperty NameProperty =
            DependencyProperty.Register("Name", typeof(string), typeof(CommandArgument), new PropertyMetadata(null));

        public object Value
        {
            get { return (object)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(object), typeof(CommandArgument), new PropertyMetadata(null));
    }

    public class CommandCollection : Dictionary<string, RelayCommand> {}
}
