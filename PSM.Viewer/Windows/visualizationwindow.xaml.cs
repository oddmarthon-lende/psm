/// <copyright file="visualizationwindow.cs" company="Baker Hughes Incorporated">
/// Copyright (c) 2015 All Rights Reserved
/// </copyright>
/// <author>Odd Marthon Lende</author>
/// <summary>Code behind for the Visualization\Chart Windows </summary>
/// 
using System;
using System.Windows;
using PSM.Viewer.Visualizations;
using System.Collections.Specialized;
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
using System.Windows.Media.Imaging;
using System.Windows.Data;
using PSM.Viewer.Utilities;
using System.Threading;
using PSM.Viewer.Models;
using System.Linq;
using System.Windows.Controls.Primitives;
using PSM.Viewer.Commands;
using PSM.Viewer.Dialogs;

namespace PSM.Viewer
{

    /// <summary>
    /// A collection of <see cref="VisualizationControl"/>
    /// </summary>
    public class VisualizationControlList : ObservableCollection<VisualizationControl> { }
    
    /// <summary>
    /// A wrapper for <see cref="System.Windows.Controls.RowDefinition"/>.
    /// Used to hide all properties other the <see cref="System.Windows.Controls.RowDefinition.Height"/> for the user in the properties window.
    /// </summary>
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

    /// <summary>
    /// A wrapper for <see cref="System.Windows.Controls.ColumnDefinition"/>.
    /// Used to hide all properties other than <see cref="System.Windows.Controls.ColumnDefinition.Width"/> for the user in the properties window.
    /// </summary>
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

    /// <summary>
    /// An observable collection of <see cref="RowDefinition"/>'s
    /// </summary>
    public class RowDefinitionList        : ObservableCollection<RowDefinition> { }

    /// <summary>
    /// An observable collection of <see cref="ColumnDefinition"/>'s
    /// </summary>
    public class ColumnDefinitionList     : ObservableCollection<ColumnDefinition> { }
    
    /// <summary>
    /// A window that can contain many <see cref="VisualizationControl"/>
    /// </summary>
    public partial  class VisualizationWindow : Theme.Window, IReload, INotifyPropertyChanged, IPropertyProvider, IDisposable
    {

        /// <summary>
        /// Store options for this context
        /// </summary>
        public StoreOptionsList StoreOptions
        {
            get
            {
                return _options.StoreOptions;
            }

            set
            {
                _options.StoreOptions = value;
            }
        }

        /// <summary>
        /// <see cref="IReload.CancellationTokenSource"/>
        /// </summary>
        public CancellationTokenSource CancellationTokenSource
        {
            get { return (CancellationTokenSource)GetValue(CancellationTokenSourceProperty); }
            set { SetValue(CancellationTokenSourceProperty, value); }
        }
        public static readonly DependencyProperty CancellationTokenSourceProperty =
            DependencyProperty.Register("CancellationTokenSource", typeof(CancellationTokenSource), typeof(VisualizationWindow), new PropertyMetadata(new CancellationTokenSource()));

        #region Properties              

        #region INotifyPropertyChanged

        /// <summary>
        /// <see cref="INotifyPropertyChanged.PropertyChanged"/>
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Triggers the <see cref="PropertyChanged"/> event
        /// </summary>
        /// <param name="propertyName"></param>
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

        /// <summary>
        /// A unique idenfifier for this window
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        private VisualizationControlList _children = null;

        /// <summary>
        /// All <see cref="VisualizationControl"/> contained within this window.
        /// </summary>
        public VisualizationControlList Children {

            get {
                return _children;
            }

            set
            {
                if(_children != null)
                    _children.CollectionChanged -= Children_CollectionChanged;

                _children = value;

                _children.CollectionChanged += Children_CollectionChanged;

                OnPropertyChanged("Children");
            }
        }
        
        private RowDefinitionList _rowdefs = null;

        /// <summary>
        /// All <see cref="RowDefinition"/> defined for this window
        /// </summary>
        public RowDefinitionList RowDefinitions
        {

            get
            {
                return _rowdefs;
            }
            set
            {
                if(_rowdefs != null)
                    _rowdefs.CollectionChanged -= Children_CollectionChanged;

                _rowdefs = value;

                _rowdefs.CollectionChanged += Children_CollectionChanged;
            }
        }

        private ColumnDefinitionList _coldefs = null;

        /// <summary>
        /// All <see cref="ColumnDefinition"/> defined for this window
        /// </summary>
        public ColumnDefinitionList ColumnDefinitions {

            get
            {
                return _coldefs;
            }
            set
            {
                if(_coldefs != null)
                    _coldefs.CollectionChanged -= Children_CollectionChanged;

                _coldefs = value;

                _coldefs.CollectionChanged += Children_CollectionChanged;
            }

        }

        /// <summary>
        /// <see cref="IReload.Status"/>
        /// </summary>
        public ReloadStatus Status
        {
            get
            {
                return (ReloadStatus)GetValue(StatusProperty);
            }

            set
            {
                SetValue(StatusProperty, value);
            }
        }
        /// <summary>
        /// Identifies the <see cref="Status"/> dependency property
        /// </summary>
        public static readonly DependencyProperty StatusProperty =
            DependencyProperty.Register("Status", typeof(ReloadStatus), typeof(VisualizationWindow), new PropertyMetadata(ReloadStatus.Idle));
       
        
        /// <summary>
        /// Re-implemented the <see cref="UIElement.InputBindings"/> property as a dependency property to prevent it from being serialized.
        /// </summary>
        public new InputBindingCollection InputBindings
        {
            get { return (InputBindingCollection)GetValue(InputBindingsProperty); }
            set { SetValue(InputBindingsProperty, value); }
        }
        /// <summary>
        /// Identifies the <see cref="VisualizationWindow.InputBindings"/> dependency property
        /// </summary>
        public static readonly DependencyProperty InputBindingsProperty =
            DependencyProperty.Register("InputBindings", typeof(InputBindingCollection), typeof(VisualizationWindow), new PropertyMetadata(null));


        /// <summary>
        /// Re-implemented the <see cref="UIElement.CommandBindings"/> property as a dependency property to prevent it from being serialized.
        /// </summary>
        public new CommandBindingCollection CommandBindings
        {
            get { return (CommandBindingCollection)GetValue(CommandBindingsProperty); }
            set { SetValue(CommandBindingsProperty, value); }
        }
        /// <summary>
        /// Identifies the <see cref="VisualizationWindow.InputBindings"/> dependency property
        /// </summary>
        public static readonly DependencyProperty CommandBindingsProperty =
            DependencyProperty.Register("CommandBindings", typeof(CommandBindingCollection), typeof(VisualizationWindow), new PropertyMetadata(null));

                
        /// <summary>
        /// When set to <c>true</c>, the mouse clicks will be captured and mouse event on the widget itself will not be fired.
        /// </summary>
        public bool CaptureRightClick
        {
            get { return (bool)GetValue(CaptureRightClickProperty); }
            set { SetValue(CaptureRightClickProperty, value); }
        }
        /// <summary>
        /// Identifies the <see cref="VisualizationWindow.CaptureRightClick"/> dependency property
        /// </summary>
        public static readonly DependencyProperty CaptureRightClickProperty =
            DependencyProperty.Register("CaptureRightClick", typeof(bool), typeof(VisualizationWindow), new PropertyMetadata(false));

        /// <summary>
        /// Hold the Tracker instance used to find which widget is under the mouse pointer.
        /// </summary>
        private Tracker _tracker = null;
                
        /// <summary>
        /// A thumbnail with a predefined size of the windows contents.
        /// </summary>
        public BitmapSource Thumbnail
        {
            get
            {
                return this.GetThumbnail();
            }
        }

        private Settings _options;
                
        #endregion

        /// <summary>
        /// The default CanExecute delegate that always returns true. 
        /// Used as parameter for commands. <see cref="RelayCommand"/>
        /// </summary>
        private bool _command_can_execute(object sender, object e) { return true; }

        #region PropertyDefinitions

        /// <summary>
        /// The property definitions for this window that will be visible to the user.
        /// </summary>
        public PropertyDefinition[] Properties { get; private set; } = new PropertyDefinition[] {
            new PropertyDefinition() {
                Category = "Common",
                TargetProperties = new List<object>(new string[] { "Title", "FontStyle", "FontFamily", "FontWeight", "FontSize", "ShowInTaskbar" })
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


        /// <summary>
        /// Gets the variables
        /// </summary>
        public IEnumerable<Models.KeyItem.Variable> Variables
        {
            get {

                return Models.KeyItem.GetGlobalVariables();
          }
        }

        private string _zoomInterval;
        /// <summary>
        /// 
        /// </summary>
        public string ZoomInterval
        {
            get
            {
                return _zoomInterval;
            }

            set
            {
                _zoomInterval = value;

                foreach (VisualizationControl c in Children)
                {
                    TimeSpan ts = c.Timespan;

                    switch (_zoomInterval)
                    {
                        case "30S":
                            ts = new TimeSpan(0, 0, 30);
                            break;
                        case "1M":
                            ts = new TimeSpan(0, 1, 0);
                            break;
                        case "30M":
                            ts = new TimeSpan(0, 30, 0);
                            break;
                        case "1H":
                            ts = new TimeSpan(1, 0, 0);
                            break;
                        case "2D":
                            ts = new TimeSpan(2, 0, 0, 0);
                            break;
                        case "4D":
                            ts = new TimeSpan(4, 0, 0, 0);
                            break;
                        case "24H":
                            ts = new TimeSpan(24, 0, 0);
                            break;
                        case "6H":
                            ts = new TimeSpan(6, 0, 0);
                            break;
                        case "1MO":
                            ts = new TimeSpan(DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month), 0, 0, 0);
                            break;
                        case "1W":
                            ts = new TimeSpan(7, 0, 0, 0);
                            break;
                        case "2W":
                            ts = new TimeSpan(14, 0, 0, 0);
                            break;
                        case "1Y":
                            ts = new TimeSpan(365, 0, 0, 0);
                            break;
                    }

                    c.Timespan = _timeSpan = ts;
                }

                ToggleButton[] buttons = zoomToolbar.Find<ToggleButton>();

                foreach(ToggleButton b in buttons)
                {
                    b.IsChecked = b.Content is string && _zoomInterval == (string)b.Content;
                }
            }
        }

        private TimeSpan _timeSpan { get; set;}

        private int _page = 0;

        /// <summary>
        /// 
        /// </summary>
        public Visibility[] ToolbarsVisibility
        {
            get
            {
                return dock.Find<ToolBar>().Select((t) => { return t.Visibility; }).ToArray();
            }

            set
            {

                Visibility[] vb = value;
                ToolBar[] tbs = dock.Find<ToolBar>();

                for(int i = 0; i < vb.Length; i++)
                {
                    tbs[i].Visibility = vb[i];
                }

            }
        }

        #endregion

        /// <summary>
        /// The default constructor
        /// </summary>
        public VisualizationWindow() : base()
        {
            
            _options = new Settings();

            Visibility = Visibility.Visible;
            ShowActivated = true;

            ColumnDefinitions = new ColumnDefinitionList();
            RowDefinitions    = new RowDefinitionList();
            Children          = new VisualizationControlList();
            Title             = String.Format("<{0}> [{1}]", GetType().Name, Id);

            #region Commands

            CreateCommands();

            #endregion

            InitializeComponent();
            
            DataContext = this;
            Closing += VisualizationWindow_Closing;

            zoomToolbar.Loaded += delegate { ZoomInterval = _zoomInterval; };

            PropertiesWindow.Created += (w) => { w.Closed += (sender, e) => Refresh(); };

            #region Bindings
            
            SetBinding(CaptureRightClickProperty, new Binding("Value")
            {
                Source = new BindingWrapper<bool>(
                    (capture) => {

                        if (_tracker == null && capture)
                        {

                                SetField(ref _tracker, _tracker == null && capture ? new Utilities.Tracker(this, Children) : null);

                                foreach (VisualizationControl element in Children)
                                {
                                    element.IsEnabled = false;
                                }

                                _tracker.MouseButtonUp += (control, e) =>
                                {

                                    if (control == null) return;
                                    if (e.ChangedButton == MouseButton.Right)
                                    {
                                        control.ContextMenu.ItemsSource = ((VisualizationControl)control).MenuItems;
                                        control.ContextMenu.IsOpen = true;
                                    }
                                };

                            }
                            else if (_tracker != null && !capture)
                            {

                                _tracker.Dispose();

                                SetField(ref _tracker, null);

                                foreach (VisualizationControl element in Children)
                                {
                                    element.IsEnabled = true;
                                }

                                this.OnReload(this);

                            }

                            return capture;
                    }),
                Mode = BindingMode.OneWayToSource
            });

            #endregion

        }

        
        private void CreateCommands()
        {
            Commands.Add("Export", new RelayCommand(ExecuteCommand, _command_can_execute, CommandType.EXPORT));
            Commands.Add("Properties", new RelayCommand(ExecuteCommand, _command_can_execute, CommandType.PROPERTIES));
            Commands.Add("PropertiesW", new RelayCommand(ExecuteCommand, _command_can_execute, CommandType.PROPERTIES_W));
            Commands.Add("Previous", new RelayCommand(ExecuteCommand, delegate { return _page > 0; }, CommandType.PREVIOUS));
            Commands.Add("Next", new RelayCommand(ExecuteCommand, _command_can_execute, CommandType.NEXT));
            Commands.Add("Reset", new RelayCommand(ExecuteCommand, delegate { return _page > 0; }, CommandType.RESET));
            Commands.Add("Refresh", new RelayCommand(ExecuteCommand, _command_can_execute, CommandType.RELOAD));
            Commands.Add("Delete", new RelayCommand(ExecuteCommand, _command_can_execute, CommandType.DELETE));
            Commands.Add("ControlsVisibility", new RelayCommand(ExecuteCommand, _command_can_execute, CommandType.CONTROLS));
            Commands.Add("AddChart", new RelayCommand(ExecuteCommand, _command_can_execute, CommandType.ADD));
            Commands.Add("Save", new RelayCommand(ExecuteCommand, _command_can_execute, CommandType.SAVE));
            Commands.Add("Zoom", new RelayCommand(ExecuteCommand, _command_can_execute, CommandType.ZOOM));
            Commands.Add("Undo", new RelayCommand(ExecuteCommand, delegate { return UndoExtension.Count > 0; }, CommandType.UNDO));
        }
        
        /// <summary>
        /// A constuctor that can be passed <see cref="VisualizationControl"/> objects as parameters and are added to the window.
        /// </summary>
        /// <param name="chart"></param>
        public VisualizationWindow(params VisualizationControl[] charts) : this()
        {

            foreach (VisualizationControl chart in charts)
            {

                if (!Children.Contains(chart))
                    Children.Add(chart);

                chart.Owner = this;
            }

        }
                        
        #region Event Handlers

        /// <summary>
        /// Shows the left/right arrows
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Widget_MouseDblClick(object sender, MouseEventArgs e)
        {

            VisualizationControl w = ((VisualizationControl)sender);
            
        }
               

        /// <summary>
        /// Called when the window close event is triggered. Cancels the close and hides instead.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void VisualizationWindow_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }

        /// <summary>
        /// Called when any of the collections (RowDefinitions, ColumnDefinitions, Children) are changed and add them to the grid.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Children_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {

            if (sender == RowDefinitions)
            {

                if (e.Action == NotifyCollectionChangedAction.Reset)

                    grid.RowDefinitions.Clear();

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

                if (e.Action == NotifyCollectionChangedAction.Reset)

                    grid.ColumnDefinitions.Clear();

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
                    foreach (VisualizationControl widget in e.NewItems)
                    {

                        widget.Owner = this;

                        widget.RegisterUserCommand();
                        widget.RegisterUserCommand("Remove", new RelayCommand(ExecuteCommand, _command_can_execute, CommandType.REMOVE_WIDGET, widget));
                        widget.RegisterUserCommand("Copy To New Window", new RelayCommand(ExecuteCommand, _command_can_execute, CommandType.TO_NEW, widget));
                        widget.RegisterUserCommand("Clone", new RelayCommand(ExecuteCommand, _command_can_execute, CommandType.CLONE, widget));

                        widget.MouseDoubleClick += Widget_MouseDblClick;

                        grid.Children.Add(widget);

                        DependencyPropertyDescriptor.FromProperty(VisualizationControl.StatusProperty, widget.GetType()).AddValueChanged(widget, Widget_StatusChanged);

                        if (ZoomInterval != null)
                            widget.Timespan = _timeSpan;
                        
                    }
            }
            
            Refresh();

        }

        private void VariableDef_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Refresh();
        }

        /// <summary>
        /// Propagates the <see cref="VisualizationControl.Status"/> up to <see cref="Status"/>.
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        public void Widget_StatusChanged(object sender, EventArgs e)
        {
            
            foreach (VisualizationControl widget in Children)
            {

                if (widget.Status != ReloadStatus.Idle)
                {
                    Status = widget.Status;
                    return;
                }                   

            }

            Status = ReloadStatus.Idle;
        }
               
        #endregion

        #region Commands

        /// <summary>
        /// Holds the commands defined for the window.
        /// </summary>
        public CommandCollection Commands { get; private set;} = new CommandCollection();

        /// <summary>
        /// Defines the different command types for the window.
        /// </summary>
        private enum CommandType
        {
            /// <summary>
            /// Remove widget from this window
            /// </summary>
            REMOVE_WIDGET = -1,
            /// <summary>
            /// Show hide controls
            /// </summary>
            CONTROLS,
            /// <summary>
            /// Export to file
            /// </summary>
            EXPORT,
            /// <summary>
            /// Show properties window
            /// </summary>
            PROPERTIES,
            /// <summary>
            /// Show widget properties window
            /// </summary>
            PROPERTIES_W,
            /// <summary>
            /// Refresh and reload everything
            /// </summary>
            RELOAD,
            /// <summary>
            /// Delete this window
            /// </summary>
            DELETE,
            /// <summary>
            /// Add a widget to this window
            /// </summary>
            ADD,
            /// <summary>
            /// Save changes
            /// </summary>
            SAVE,
            /// <summary>
            /// Undo
            /// </summary>
            UNDO,
            /// <summary>
            /// Move widget to new window
            /// </summary>
            TO_NEW,
            /// Zoom widgets
            ZOOM,
            
            PREVIOUS,
            NEXT,
            RESET,
            CLONE
        }
               
        
        /// <summary>
        /// The execute event handler that is called by <see cref="RelayCommand"/>
        /// </summary>
        /// <param name="sender">The <see cref="RelayCommand"/></param>
        /// <param name="parameter">An optional parameter passed in through XAML.</param>
        private void ExecuteCommand(object sender, object parameter)
        {
            VisualizationControl w;
            Window window;
            RelayCommand cmd = (RelayCommand)sender;
            Stream stream = null;

            switch ((CommandType)cmd.Arguments[0].Value)
            {

                case CommandType.TO_NEW:
                case CommandType.CLONE:

                    w = (VisualizationControl)cmd.Arguments[1].Value;
                    stream = new MemoryStream();
                    w.Export(stream);
                    stream.Seek(0, SeekOrigin.Begin);
                    break;
            }

            switch ((CommandType)cmd.Arguments[0].Value)
            {

                case CommandType.TO_NEW:                    

                    App.Current.Dispatcher.Invoke(delegate
                    {

                        ((MainWindow)App.Current.MainWindow).Create(v =>
                        {                            
                            v.Children.Add((VisualizationControl)System.Windows.Markup.XamlReader.Load(stream));
                            stream.Dispose();
                            v.OnReload(v);
                        });

                    });                                      

                    break;

                case CommandType.CLONE:

                    Children.Add((VisualizationControl)System.Windows.Markup.XamlReader.Load(stream));
                    break;

                case CommandType.UNDO:

                    UndoExtension.Undo();
                    break;

                case CommandType.REMOVE_WIDGET:

                    w = (VisualizationControl)cmd.Arguments[1].Value;

                    Children.Remove(w);
                    w.Dispose();

                    break;

                case CommandType.SAVE:
                    
                    IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForDomain();

                    if (!store.DirectoryExists("windows"))
                        store.CreateDirectory("windows");

                    using (stream = store.OpenFile(String.Format(@"windows\{0}", String.Format("{0}.xaml", this.Id)), FileMode.Create))
                    {
                        this.Export(stream);
                    }

                    break;

                case CommandType.ADD:

                    VisualizationControl chart = (VisualizationControl)Activator.CreateInstance(((VisualizationControl.InheritorInfo)parameter).Type);
                    
                    chart.Owner = this;

                    Children.Add(chart);

                    this.OnReload(chart);

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
                        this.Export(dialog.OpenFile());
                    }

                    break;

                case CommandType.DELETE:

                    App.Current.Dispatcher.InvokeAsync(delegate
                    {
                        ((MainWindow)App.Current.MainWindow).Remove(this);
                    });

                    break;

                case CommandType.RELOAD:
                    
                    break;

                case CommandType.PROPERTIES:
                    
                    window = (new PropertiesWindow(this, _options.Store)
                    {
                        Title = String.Format("Properties [{0}]", this.Title),
                        ShowInTaskbar = false,
                        Owner = this,
                        Width = SystemParameters.FullPrimaryScreenWidth * .5,
                        Height = SystemParameters.FullPrimaryScreenHeight * .5,
                        WindowStartupLocation = WindowStartupLocation.CenterScreen
                    });

                    PropertyGrid grid = ((PropertiesWindow)window).PropertyGrids.ToArray()[1];
                    
                    grid.SelectedObject = null;
                                        
                    Dispatcher.InvokeAsync(delegate
                    {
                        grid.SelectedObjectChanged += settings_propertyGrid_SelectedObjectChanged;
                        grid.SelectedObject = _options.Store;

                    }, System.Windows.Threading.DispatcherPriority.Background);

                    window.ShowDialog();
                    
                    
                    break;

                case CommandType.ZOOM:

                    ToggleButton button = (ToggleButton)parameter;
                    ToggleButton[] buttons = zoomToolbar.Find<ToggleButton>();

                    if (button.IsChecked.HasValue && !button.IsChecked.Value)
                    {
                        ZoomInterval = null;
                        break;
                    }                       

                    ZoomInterval =  (string)button.Content;

                    

                    break;

                case CommandType.PREVIOUS:

                    Previous();
                    break;

                case CommandType.NEXT:

                    Next();
                    break;

                case CommandType.RESET:

                    Reset();
                    break;

                default:

                    break;
            }

            Refresh();
            this.OnReload(this);
        }

        #endregion

        private void settings_propertyGrid_SelectedObjectChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            _options.settings_propertyGrid_SelectedObjectChanged(sender, e);
        }
        
        /// <summary>
        /// Used to specify that content should not be serialized when serialzing to XAML.
        /// </summary>
        /// <returns><c>false</c> so that content will not be serialized to XAML</returns>
        public override bool ShouldSerializeContent()
        {
            return false;
        }

        /// <summary>
        /// Used to disable serialization of selected properties to XAML.
        /// </summary>
        /// <param name="dp">The Dependency Property</param>
        /// <returns><c>true</c> if the property should be serialize, <c>false</c> if not.</returns>
        protected override bool ShouldSerializeProperty(DependencyProperty dp)
        {
            
            DependencyProperty[] properties = new DependencyProperty[]
            {
                IconProperty,
                CommandBindingsProperty,
                InputBindingsProperty,
                TopmostProperty,
                NameProperty,
                CaptureRightClickProperty,
                ShowActivatedProperty,
                StatusProperty,
                TemplateProperty,
                VisibilityProperty,
                CursorProperty,
                AllowsTransparencyProperty,
                WindowStyleProperty,
                StyleProperty,
                CanEnterFullscreenProperty,
                CancellationTokenSourceProperty

            };

            foreach(DependencyProperty p in properties)
            {
                if (dp == p)
                    return false;
            }
            
            return base.ShouldSerializeProperty(dp);
        }
        
        public void Refresh()
        {
            
            foreach (VisualizationControl widget in Children)
            {
                widget.Refresh();
            }

        }

        /// <summary>
        /// Reloads everything in the window.
        /// </summary>
        public void Reload()
        {
            
            foreach (VisualizationControl chart in Children)
            {
                chart.Reload();
            }
   
        }

        /// <summary>
        /// Overrides what should be displayed if the object is converted to text.
        /// </summary>
        /// <returns>The title</returns>
        public override string ToString()
        {
            return String.IsNullOrEmpty(Title) ? String.Format("<{0}> [{1}]", GetType().Name, Id) : Title;
        }

        /// <summary>
        /// Move all children to next results.
        /// </summary>
        /// <returns></returns>
        public bool Next()
        {
            bool r = false;

            foreach (VisualizationControl widget in Children)
                r |= widget.Next();

            ++_page;

            return _page < 0;
        }

        /// <summary>
        /// Move all children to previous results.
        /// </summary>
        /// <returns></returns>
        public bool Previous()
        {
            bool r = false;

            foreach (VisualizationControl widget in Children)
                r |= widget.Previous();
            
            _page = Math.Max(0, --_page);

            return r;
        }

        public void Reset()
        {
            _page = 0;
            foreach (VisualizationControl widget in Children)
                widget.Reset();

        }
        
        /// <summary>
        /// <see cref="IDisposable.Dispose"/>
        /// </summary>
        public void Dispose()
        {

            foreach (VisualizationControl w in Children)
            {
                w.Dispose();
            }

            Children.Clear();

            // If there is a store linked to the dispatcher, dispose of it
            // IMPORTANT : Dispose of the store last, if not a new instance is created in the pool.
            Store.Get(Dispatcher).Dispose();

            // Shut down the dispatcher to kill the UI threads.
            Dispatcher.InvokeShutdown();
                        
        }
        
        private void variable_combos_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            ComboBox combo = (ComboBox)sender;
            KeyItem.Variable variable = (KeyItem.Variable)combo.DataContext;

            if (!combo.IsDropDownOpen)
                variable.Reload();
            
        }

        private void variable_combos_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.OnReload(this);
        }
    }
}
