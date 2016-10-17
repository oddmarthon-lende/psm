/// <copyright file="commands.cs" company="Baker Hughes Incorporated">
/// Copyright (c) 2015 All Rights Reserved
/// </copyright>
/// <author>Odd Marthon Lende</author>
/// <summary>Commands</summary>
/// 

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace PSM.Viewer.Commands
{

    /// <summary>
    /// Relays to delegate methods 
    /// </summary>
    public class RelayCommand : CommandParameter, ICommand
    {

        /// <summary>
        /// Contructor
        /// </summary>
        public RelayCommand() : base() { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="execute">The delegate that will be called when the command is executed. <see cref="Execute(object)"/></param>
        /// <param name="canExecute">The delegate that will be called when the commands <see cref="CanExecute(object)"/> method is called</param>
        /// <param name="args">Optional parameters <see cref="CommandParameter"/></param>
        public RelayCommand(Action<object, object> execute, Func<object, object, bool> canExecute, params object[] args) : base(args)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        /// <summary>
        /// The <see cref="Execute(object)"/> delegate
        /// </summary>
        private Action<object, object> _execute { get; set; } = null;

        /// <summary>
        /// The <see cref="CanExecute(object)"/> delegate
        /// </summary>
        private Func<object, object, bool> _canExecute { get; set; } = null;
               
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        /// <summary>
        /// Calls the delegate. <see cref=""/>
        /// </summary>
        /// <param name="parameter">Optional</param>
        /// <returns><c>true</c> if it can, <c>false</c> if not</returns>
        public bool CanExecute(object parameter)
        {

            if (_canExecute != null)
                return _canExecute(this, parameter);

            return parameter != null;

        }

        /// <summary>
        /// Calls the delegate.
        /// </summary>
        /// <param name="parameter">Optional</param>
        public void Execute(object parameter)
        {
            if (_execute != null)
                _execute(this, parameter);
        }

    }

    /// <summary>
    /// A list of <see cref="CommandArgument"/>
    /// </summary>
    public class CommandArgumentsList : List<CommandArgument> {}

    /// <summary>
    /// A collection of <see cref="RelayCommand"/>'s
    /// </summary>
    public class CommandCollection : Dictionary<string, RelayCommand> { }

    /// <summary>
    /// Represents a command parameter.
    /// </summary>
    public class CommandParameter : DependencyObject
    {

        /// <summary>
        /// Constructor
        /// </summary>
        public CommandParameter() {}

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="args">Optional arguments</param>
        public CommandParameter(params object[] args)
        {
            foreach (object arg in args)
            {
                this.Arguments.Add(new CommandArgument(arg));
            }
        }

        /// <summary>
        /// Gets the arguments list
        /// </summary>
        public CommandArgumentsList Arguments { get; private set; } = new CommandArgumentsList();

    }

    /// <summary>
    /// Represents a command argument
    /// </summary>
    public class CommandArgument : DependencyObject
    {

        /// <summary>
        /// Constructor
        /// </summary>
        public CommandArgument() { }


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="value"><see cref="Value"/></param>
        public CommandArgument(object value)
        {
            this.Value = value;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name"><see cref="Name"/></param>
        /// <param name="value"><see cref="Value"/></param>
        public CommandArgument(string name, object value) : this(value)
        {
            this.Name = name;
        }

        /// <summary>
        /// The name of this argument
        /// </summary>
        public string Name
        {
            get { return (string)GetValue(NameProperty); }
            set { SetValue(NameProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="Name"/> dependency property
        /// </summary>
        public static readonly DependencyProperty NameProperty =
            DependencyProperty.Register("Name", typeof(string), typeof(CommandArgument), new PropertyMetadata(null));

        /// <summary>
        /// The argument value
        /// </summary>
        public object Value
        {
            get { return (object)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }
        /// <summary>
        /// Identifies the <see cref="Value"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(object), typeof(CommandArgument), new PropertyMetadata(null));
    }

    
}
