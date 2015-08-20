using System;
using System.Windows;
using PSMViewer.Visualizations;
using System.Collections.Specialized;
using PSMViewer.ViewModels;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Windows.Input;
using System.Windows.Controls;
using Microsoft.Win32;
using Xceed.Wpf.Toolkit.PropertyGrid;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.IO.IsolatedStorage;
using System.IO;

namespace PSMViewer
{

    public class VisualizationControlList : ObservableCollection<VisualizationControl> { }

    public class RowDefinition
    {
        public System.Windows.Controls.RowDefinition Source { get; private set; }

        public GridLength Height
        {

            get
            {
                return Source.Height;
            }

            set
            {
                Source.Height = value;
            }
        }

        public RowDefinition() : this(null) { }

        public RowDefinition(System.Windows.Controls.RowDefinition Source = null)
        {
            this.Source = Source??new System.Windows.Controls.RowDefinition();
        }

        public static explicit operator RowDefinition(System.Windows.Controls.RowDefinition Source)
        {
            return new RowDefinition(Source);
        }

        public static explicit operator System.Windows.Controls.RowDefinition(RowDefinition Wrapped)
        {
            return Wrapped.Source;
        }
    }
    public class ColumnDefinition
    {
        public System.Windows.Controls.ColumnDefinition Source { get; private set; }

        public GridLength Width {

            get
            {
                return Source.Width;
            }

            set
            {
                Source.Width = value;
            }
        }

        public ColumnDefinition() : this(null) { }

        public ColumnDefinition(System.Windows.Controls.ColumnDefinition Source = null)
        {
            this.Source = Source??new System.Windows.Controls.ColumnDefinition();
        }

        public static explicit operator ColumnDefinition(System.Windows.Controls.ColumnDefinition Source)
        {
            return new ColumnDefinition(Source);
        }

        public static explicit operator System.Windows.Controls.ColumnDefinition(ColumnDefinition Wrapped)
        {
            return Wrapped.Source;
        }
    }

    public class RowDefinitionList        : ObservableCollection<RowDefinition> { }
    public class ColumnDefinitionList     : ObservableCollection<ColumnDefinition> { }
        
    public partial class VisualizationWindow : Window, IReload, INotifyPropertyChanged
    {
        
        #region Properties

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion

        public Guid Id { get; set; } = Guid.NewGuid();

        private VisualizationControlList children = new VisualizationControlList();
        public VisualizationControlList Children
        {
            get
            {
                return children;
            }

            set
            {
                children = value;
            }
        }

        public Visibility ControlsVisibility
        {
            get {
                return WindowStyle == WindowStyle.None ? Visibility.Hidden : Visibility.Visible;
            }

            set {

                if (EqualityComparer<Visibility>.Default.Equals(WindowStyle == WindowStyle.None ? Visibility.Hidden : Visibility.Visible, value)) return;

                WindowStyle = value == Visibility.Hidden ? WindowStyle.None : WindowStyle.SingleBorderWindow;

                OnPropertyChanged("ControlsVisibility");

            }
        }

        public RowDefinitionList RowDefinitions { get; set; } = new RowDefinitionList();

        public ColumnDefinitionList ColumnDefinitions { get; set; } = new ColumnDefinitionList();

        #region PropertyDefinitions

        private static PropertyDefinition[] Properties = new PropertyDefinition[] {
            new PropertyDefinition() {
                Category = "Common",
                TargetProperties = new List<object>(new string[] { "Title", "FontStyle", "FontFamily", "FontWeight", "FontSize" })
            },
            new PropertyDefinition() {
                Category = "Common",
                IsExpandable = true,
                TargetProperties = new List<object>(new string[] { "Background", "Foreground" })
            },
            new PropertyDefinition() {
                 
                Category = "Layout",
                TargetProperties = new List<object>(new string[] { "RowDefinitions", "ColumnDefinitions" })
            }
        };

        public new InputBindingCollection InputBindings
        {
            get { return (InputBindingCollection)GetValue(InputBindingsProperty); }
            set { SetValue(InputBindingsProperty, value); }
        }
        public static readonly DependencyProperty InputBindingsProperty =
            DependencyProperty.Register("InputBindings", typeof(InputBindingCollection), typeof(VisualizationWindow), new PropertyMetadata(null));


        public new CommandBindingCollection CommandBindings
        {
            get { return (CommandBindingCollection)GetValue(CommandBindingsProperty); }
            set { SetValue(CommandBindingsProperty, value); }
        }
        public static readonly DependencyProperty CommandBindingsProperty =
            DependencyProperty.Register("CommandBindings", typeof(CommandBindingCollection), typeof(VisualizationWindow), new PropertyMetadata(null));


        #endregion

        private Utilities.Tracker _tracker = null;
        public bool CaptureRightClick {

            get {
                return _tracker != null;
            }

            set {

                if (_tracker == null && value) {

                    SetField(ref _tracker, _tracker == null && value ? new Utilities.Tracker(this, Children) : null);

                    foreach(VisualizationControl element in Children)
                    {
                        element.IsEnabled = false;
                    }

                    _tracker.MouseButtonUp += (control, e) => {

                        if (control == null) return;
                        if (e.ChangedButton == MouseButton.Right)
                        {
                            control.ContextMenu.ItemsSource = ((VisualizationControl)control).MenuItems;
                            control.ContextMenu.IsOpen = true;
                        }
                     };
                    
                }
                else if(_tracker != null && !value)
                {

                    _tracker.Dispose();

                    SetField(ref _tracker, null);

                    foreach (VisualizationControl element in Children)
                    {
                        element.IsEnabled = true;
                    }

                }
            }
        }

        #endregion

        private Func<object, object, bool> canExecute = delegate { return true; };

        public VisualizationWindow() : base()
        {           

            #region Commands

            Commands.Add("Export", new RelayCommand(ExecuteCommand, canExecute, CommandType.EXPORT));
            Commands.Add("Properties", new RelayCommand(ExecuteCommand, canExecute, CommandType.PROPERTIES));
            Commands.Add("Refresh", new RelayCommand(ExecuteCommand, canExecute, CommandType.REFRESH));
            Commands.Add("Delete", new RelayCommand(ExecuteCommand, canExecute, CommandType.DELETE));
            Commands.Add("ControlsVisibility", new RelayCommand(ExecuteCommand, canExecute, CommandType.CONTROLS));
            Commands.Add("AddChart", new RelayCommand(ExecuteCommand, canExecute, CommandType.ADD));
            Commands.Add("Save", new RelayCommand(ExecuteCommand, canExecute, CommandType.SAVE));

            #endregion

            InitializeComponent();

            children.CollectionChanged += Children_CollectionChanged;

            RowDefinitions.CollectionChanged += Children_CollectionChanged;
            ColumnDefinitions.CollectionChanged += Children_CollectionChanged;

            this.DataContext = this;

            this.Closing += VisualizationWindow_Closing;
            this.KeyDown += VisualizationWindow_KeyDown_LCTRL;
            this.KeyUp   += VisualizationWindow_KeyUp_LCTRL;
            
        }
        
        #region Event Handlers

        private void VisualizationWindow_KeyUp_LCTRL(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.LeftCtrl) return;

            CaptureRightClick = false;

            foreach (VisualizationControl widget in Children)
            {
                widget.NavigationVisibility = Visibility.Collapsed;
            }

            Reload();

        }

        private void VisualizationWindow_KeyDown_LCTRL(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.LeftCtrl) return;

            CaptureRightClick = true;

            _tracker.MouseOver += (current) =>
            {
                current.IsEnabled = true;
                ((VisualizationControl)current).NavigationVisibility = Visibility.Visible;
            };

            _tracker.MouseOut += (previous) =>
            {
                previous.IsEnabled = false;
                ((VisualizationControl)previous).NavigationVisibility = Visibility.Collapsed;
            };

        }

        private void VisualizationWindow_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }

        private void Children_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {

            if (sender == RowDefinitions)
            {

                if (e.OldItems != null)
                    foreach (RowDefinition d in e.OldItems)
                    {
                        grid.RowDefinitions.Remove(d.Source);
                    }

                if (e.NewItems != null)
                    foreach (RowDefinition d in e.NewItems)
                    {
                        if (!grid.RowDefinitions.Contains(d.Source))
                            grid.RowDefinitions.Add(d.Source);
                    }
            }
            else if (sender == ColumnDefinitions)
            {

                if (e.OldItems != null)
                    foreach (ColumnDefinition d in e.OldItems)
                    {
                        grid.ColumnDefinitions.Remove(d.Source);
                    }

                if (e.NewItems != null)
                    foreach (ColumnDefinition d in e.NewItems)
                    {
                        if(!grid.ColumnDefinitions.Contains(d.Source))
                            grid.ColumnDefinitions.Add(d.Source);
                    }
            }
            else
            {

                if (e.OldItems != null)
                    foreach (VisualizationControl v in e.OldItems)
                    {
                        grid.Children.Remove(v);
                    }

                if (e.NewItems != null)
                    foreach (VisualizationControl v in e.NewItems)
                    {
                        v.Owner = this;

                        v.RegisterUserCommand();
                        v.RegisterUserCommand("Remove", new RelayCommand(ExecuteCommand, canExecute, CommandType.REMOVE_WIDGET, v));

                        grid.Children.Add(v);

                    }
            }

            foreach(VisualizationControl widget in Children)
            {
                widget.Refresh();
            }

        }

        #endregion

        #region Commands

        public CommandCollection Commands { get; private set;} = new CommandCollection();

        private enum CommandType
        {
            CONTROLS,
            EXPORT,
            PROPERTIES,
            REFRESH,
            DELETE,
            ADD,
            SAVE,
            REMOVE_WIDGET = -1
        }

        private void ExecuteCommand(object sender, object parameter)
        {

            RelayCommand cmd = (RelayCommand)sender;

            switch ((CommandType)cmd.Arguments[0].Value)
            {
                case CommandType.REMOVE_WIDGET:

                    Children.Remove((VisualizationControl)cmd.Arguments[1].Value);
                    break;

                case CommandType.SAVE:

                    IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForDomain();

                    if (!store.DirectoryExists("windows"))
                        store.CreateDirectory("windows");

                    using (IsolatedStorageFileStream stream = store.OpenFile(String.Format(@"windows\{0}", String.Format("{0}.xaml", this.Id)), FileMode.Create))
                    {
                        MainWindow.Export(this, stream);
                    }

                    break;

                case CommandType.ADD:

                    VisualizationControl chart = (VisualizationControl)Activator.CreateInstance(((VisualizationControl.InheritorInfo)parameter).Type);

                    chart.Owner = this;

                    Children.Add(chart);
                    ((MainWindow)App.Current.MainWindow).Reload(chart);

                    foreach(object item in chart.ContextMenu.Items)
                    {
                        MenuItem m_item;

                        try
                        {
                            m_item = (MenuItem)item;
                        }
                        catch(InvalidCastException)
                        {
                            continue;
                        }

                        if ((string)m_item.Header == "Properties")
                        {
                            m_item.Command.Execute(null);
                        }
                    }

                    break;

                case CommandType.EXPORT:

                    SaveFileDialog dialog = new SaveFileDialog()
                    {
                        FileName = this.Id.ToString(),
                        DefaultExt = MainWindow.DefaultExt,
                        Filter = MainWindow.Filter,
                        RestoreDirectory = true
                    };

                    if (dialog.ShowDialog(this).Value == true)
                    {
                        MainWindow.Export(this, dialog.OpenFile());
                    }

                    break;

                case CommandType.DELETE:

                    ((MainWindow)App.Current.MainWindow).Remove(this);
                    break;

                case CommandType.REFRESH:

                    ((MainWindow)App.Current.MainWindow).Reload(this);
                    break;

                case CommandType.PROPERTIES:

                    (new PropertiesWindow(this, Properties) {
                        Title = String.Format("Properties [{0}]", this.Title),
                        ShowInTaskbar = false,
                        Owner = this,
                        Width = this.ActualHeight * .75,
                        Height = this.ActualWidth * .75
                    }).ShowDialog();

                    break;

                case CommandType.CONTROLS:

                    ControlsVisibility = ControlsVisibility == Visibility.Hidden ? Visibility.Visible : Visibility.Hidden;

                    break;

                default:
                    break;
            }

        }      

        #endregion 

        public VisualizationWindow(VisualizationControl chart) : this()
        {            

            if (!Children.Contains(chart))
                Children.Add(chart);

            chart.Owner = this;

        }

        public override bool ShouldSerializeContent()
        {
            return false;
        }

        protected override bool ShouldSerializeProperty(DependencyProperty dp)
        {
            
            switch(dp.Name)
            {

                case "Owner":
                case "Icon":
                case "CommandBindings":
                case "InputBindings":
                    return false;

            }

            return base.ShouldSerializeProperty(dp);
        }
        
        public void Reload()
        {
            foreach(IReload chart in Children)
            {
                chart.Reload();
            }
        }

        public override string ToString()
        {
            return this.Title;
        }

        public bool Next()
        {
            bool r = false;

            foreach (VisualizationControl widget in Children)
                r |= widget.Next();

            return r;
        }

        public bool Previous()
        {
            bool r = false;

            foreach (VisualizationControl widget in Children)
                r |= widget.Previous();

            return r;
        }
        
    }
}
