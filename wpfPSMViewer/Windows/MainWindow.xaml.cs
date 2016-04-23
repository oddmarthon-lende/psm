﻿/// <copyright file="mainwindow.xaml.cs" company="Baker Hughes Incorporated">
/// Copyright (c) 2015 All Rights Reserved
/// </copyright>
/// <author>Odd Marthon Lende</author>
/// <summary>Code behind for the Main Window</summary>

using PSMViewer.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using PSMViewer.Models;
using PSMViewer.Visualizations;
using System.Windows.Markup;
using System.IO;
using System.IO.IsolatedStorage;
using System.Windows.Threading;
using Microsoft.Win32;
using System.Linq;
using Xceed.Wpf.AvalonDock.Layout;
using System.Threading;
using System.Windows.Data;
using PSMViewer.Utilities;
using Xceed.Wpf.Toolkit.PropertyGrid;
using PSMonitor.Stores;
using PSMViewer.Editors;
using System.Windows.Media.Imaging;

namespace PSMViewer
{

    public sealed partial class MainWindow : Window, INotifyPropertyChanged, IReload
    {
        
        
        /// <summary>
        /// <see cref="IReload.CancellationTokenSource"/>
        /// </summary>
        public CancellationTokenSource CancellationTokenSource { get; set; } = new CancellationTokenSource();

        #region Static Properties and Methods

        /// <summary>
        /// The default extension in file dialogs
        /// </summary>
        public static string DefaultExt = ".psm";
        
        /// <summary>
        /// The default filter in file dialogs
        /// </summary>
        public static string Filter = "PSM documents (.psm)|*.psm";

        /// <summary>
        /// A format string used with <see cref="string.Format(string, object[])"/> to format the path to stored windows.
        /// </summary>
        static string WindowsFolderFormat = @"windows\{0}";

        /// <summary>
        /// The isolated storage file store for this application.
        /// </summary>
        static IsolatedStorageFile UserStore = IsolatedStorageFile.GetUserStoreForDomain();
               

        #endregion

        #region INotifyPropertyChanged

        /// <summary>
        /// <see cref="INotifyPropertyChanged.PropertyChanged"/>
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Triggers the <see cref="INotifyPropertyChanged.PropertyChanged"/> event
        /// </summary>
        /// <param name="propertyName">The property name that was changed</param>
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Set a fields value 
        /// </summary>
        /// <typeparam name="T">The field type</typeparam>
        /// <param name="field">The field</param>
        /// <param name="value">The value</param>
        /// <param name="propertyName">Optional property name. If called from setter, uses name of property automatically.</param>
        /// <returns>A fart</returns>
        private bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion


        /// <summary>
        /// Used to show a status in the bottom of the window
        /// </summary>
        public ReloadStatus Status
        {
            get { return (ReloadStatus)GetValue(StatusProperty); }
            set { SetValue(StatusProperty, value); }
        }
        /// <summary>
        /// Identifies the <see cref="Status"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty StatusProperty =
            DependencyProperty.Register("Status", typeof(ReloadStatus), typeof(MainWindow), new PropertyMetadata(ReloadStatus.Idle));

        

        private ObservableCollection<VisualizationWindow> _windows = new ObservableCollection<VisualizationWindow>();
        /// <summary>
        /// Contains all the data visualization windows 
        /// </summary>
        public IEnumerable<WindowInfo> Windows
        {

            get
            {

                return _windows.Select(w => {
                    return new WindowInfo(w);
                });

            }
            
        }

        /// <summary>
        /// Gets the options object
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Settings Options
        {
            get { return (Settings)GetValue(OptionsProperty); }
            set { SetValue(OptionsProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="Options"/> dependency property
        /// </summary>
        public static readonly DependencyProperty OptionsProperty =
            DependencyProperty.Register("Options", typeof(Settings), typeof(MainWindow), new PropertyMetadata(null));


        private EventLogWindow EventLogWindow;


        /// <summary>
        /// Used to identify commands
        /// </summary>
        private enum CommandType
        {
            /// <summary>
            /// Show the about dialog.
            /// </summary>
            ABOUT,
            /// <summary>
            /// Import XAML files
            /// </summary>
            IMPORT,
            /// <summary>
            /// Refresh the table
            /// </summary>
            REFRESH_TABLE,
            /// <summary>
            /// Refresh the key tree
            /// </summary>
            REFRESH_TREE,
            /// <summary>
            /// Move to next result set.
            /// </summary>
            NEXT,
            /// <summary>
            /// Move to previous result set.
            /// </summary>
            PREVIOUS,
            /// <summary>
            /// Activate a window
            /// </summary>
            WINDOWS,
            /// <summary>
            /// Exit the application
            /// </summary>
            EXIT,
            /// <summary>
            /// Save everything to disk
            /// </summary>
            SAVE,
            /// <summary>
            /// Set the chart type for a key, so that it will automatically use this type whenever data is loaded into the main window.
            /// </summary>
            SET_CHART_TYPE,
            /// <summary>
            /// Create a new window
            /// </summary>
            NEW_WINDOW,
            /// <summary>
            /// Stop receiving realtime updates.
            /// </summary>
            STOP,
            /// <summary>
            /// Show the event log
            /// </summary>
            EVENT_LOG,
            /// <summary>
            /// Opens the properties for the selected visualization control
            /// </summary>
            OPEN_VISUALIZATION_PROPERTIES,
            /// <summary>
            /// Add the key to the current visualizatio
            /// </summary>
            ADD_KEY_CHART,
            /// <summary>
            /// Remove key from chart
            /// </summary>
            REMOVE_KEY_CHART
        }

        private VisualizationControl _graph = null;

        /// <summary>
        /// The MainWindow Constructor
        /// </summary>
        public MainWindow()
        {
            
            Options = new Settings(this);
            
            CreateCommands();

            InitializeComponent();

            Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            NameScope.SetNameScope(treeContextMenu, NameScope.GetNameScope(this));

            Loaded += (sender, e) => this.OnReload(this);
            Closing += (sender, e) => Commands["Exit"].Execute(null);
            _windows.CollectionChanged += (sender, e) => OnPropertyChanged("Windows");

            DataContext = new MultiControl(null, this.OnReload);

            ((MultiControl)DataContext).Get().Activate(this);
            ((MultiControl)DataContext).Forward(this);            

            treeView.Forward(this);
            
            // Bind to the treeView Key property and update the data context
            treeView.SetBinding(Tree.KeyProperty, new Binding("Value")
            {
                Source = new BindingWrapper<KeyItem>(

                    key =>
                    {

                        MultiControl ctrl = (MultiControl)this.DataContext;

                        if (key != null)
                        {

                            ctrl.Entries.Clear();

                            metaDataGrid.ItemsSource = PSMonitor.PSM.Store(Dispatcher).Meta(key.Path);
                            Title = key.ToString();

                            if (key.Type == null)
                                return key;

                            ctrl.Key = key;
                            
                            Visualize(key);
                            this.OnReload(ctrl);

                        }                        

                        return key;

                    }),

                Mode = BindingMode.OneWayToSource

            });

            treeView.KeyRightClick += TreeView_KeyRightClick;

            EventLogWindow = new EventLogWindow() { };
            EventLogWindow.Closing += EvtWindow_Closing;
            
        }

        private void CreateCommands()
        {

            Func<object, object, bool> canExecute = delegate { return true; };

            Commands.Add("Windows", new RelayCommand(ExecuteCommand, canExecute, CommandType.WINDOWS));
            Commands.Add("Import", new RelayCommand(ExecuteCommand, canExecute, CommandType.IMPORT));
            Commands.Add("Save", new RelayCommand(ExecuteCommand, canExecute, CommandType.SAVE));
            Commands.Add("Exit", new RelayCommand(ExecuteCommand, canExecute, CommandType.EXIT));
            Commands.Add("About", new RelayCommand(ExecuteCommand, canExecute, CommandType.ABOUT));
            Commands.Add("RefreshTable", new RelayCommand(ExecuteCommand, canExecute, CommandType.REFRESH_TABLE));
            Commands.Add("RefreshTree", new RelayCommand(ExecuteCommand, canExecute, CommandType.REFRESH_TREE));
            Commands.Add("SetChartType", new RelayCommand(ExecuteCommand, ContextMenu_CanExecute, CommandType.SET_CHART_TYPE));
            Commands.Add("Next", new RelayCommand(ExecuteCommand, canExecute, CommandType.NEXT));
            Commands.Add("Previous", new RelayCommand(ExecuteCommand, canExecute, CommandType.PREVIOUS));
            Commands.Add("NewWindow", new RelayCommand(ExecuteCommand, canExecute, CommandType.NEW_WINDOW));
            Commands.Add("Stop", new RelayCommand(ExecuteCommand, canExecute, CommandType.STOP));
            Commands.Add("EventLog", new RelayCommand(ExecuteCommand, canExecute, CommandType.EVENT_LOG));
            Commands.Add("OpenChartProperties", new RelayCommand(ExecuteCommand, canExecute, CommandType.OPEN_VISUALIZATION_PROPERTIES));
            Commands.Add("AddKeyToChart", new RelayCommand(ExecuteCommand, canExecute, CommandType.ADD_KEY_CHART));
            Commands.Add("RemoveKeyFromChart", new RelayCommand(ExecuteCommand, canExecute, CommandType.REMOVE_KEY_CHART));
        }
        
        private void TreeView_KeyRightClick(Tree sender, KeyItem key, System.Windows.Input.MouseEventArgs args)
        {

            if (key.Type != null)
            {

                ContextMenu menu = new ContextMenu();

                MenuItem item = new MenuItem();

                if(_graph != null)
                {

                    if (_graph.Keys.Contains(key.Path))
                    {
                        item.Command = Commands["RemoveKeyFromChart"];
                        item.Header = "Remove From Visualization";
                    }
                    else
                    {
                        item.Command = Commands["AddKeyToChart"];
                        item.Header = "Add To Visualization";
                    }

                    item.CommandParameter = key;
                    menu.Items.Add(item);

                    
                }

                menu.IsOpen = true;
            }
               
        }

        private void EvtWindow_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
            ((EventLogWindow)sender).Hide();
        }

        #region Commands

        /// <summary>
        /// Stores the commands defined for this window
        /// </summary>
        public CommandCollection Commands { get; private set; } = new CommandCollection();

        /// <summary>
        /// Executes the commands based on CommandType argument passed to RelayCommand objects
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="parameter"></param>
        private void ExecuteCommand(object sender, object parameter)
        {

            RelayCommand cmd = (RelayCommand)sender;
            KeyItem key = (KeyItem)treeView.SelectedValue;
            Window window = null;

            switch ((CommandType)cmd.Arguments[0].Value)
            {
                case CommandType.REMOVE_KEY_CHART:

                    if(_graph != null)
                    {
                        _graph.Remove((KeyItem)parameter);
                    }

                    break;

                case CommandType.ADD_KEY_CHART:

                    if(_graph != null)
                    {
                        RefreshSettingsValues(_graph.Add((KeyItem)parameter));
                    }                        

                    break;

                case CommandType.OPEN_VISUALIZATION_PROPERTIES:

                    _graph.Commands["Properties"].Execute(null);
                    break;

                case CommandType.EVENT_LOG:

                    EventLogWindow.Show();
                    break;

                case CommandType.STOP:

                    
                    break;

                
                case CommandType.NEW_WINDOW:

                    Create();
                    break;

                case CommandType.WINDOWS:

                    window = (VisualizationWindow)((WindowInfo)parameter).Window;

                    window.Dispatcher.InvokeAsync(window.Show);
                    window.Dispatcher.InvokeAsync(window.Focus);

                    break;

                case CommandType.IMPORT:

                    OpenFileDialog dialog = new OpenFileDialog()
                    {
                        DefaultExt = MainWindow.DefaultExt,
                        Filter = MainWindow.Filter,
                        RestoreDirectory = true
                    };

                    if (dialog.ShowDialog().Value == true)
                    {
                        Import(dialog.OpenFile());
                    }

                    break;

                case CommandType.SAVE:

                    IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForDomain();
                    
                    foreach (VisualizationWindow w in _windows)
                    {
                                                
                        using (IsolatedStorageFileStream stream = store.OpenFile(String.Format(WindowsFolderFormat, String.Format("{0}.xaml", w.Id)), FileMode.Create))
                        {
                            w.Dispatcher.Invoke(delegate
                            {
                                w.Export(stream);
                            });
                        }
                    }

                    foreach (object element in _layoutRoot.Descendents())
                    {
                        if (element is LayoutAnchorable)
                        {
                            
                            LayoutAnchorable anchorable = (LayoutAnchorable)element;
                            
                            using (IsolatedStorageFileStream stream = store.OpenFile(String.Format(@"state\{0}.xaml", anchorable.ContentId), FileMode.Create))
                            {
                                
                                new LayoutAnchorableSavedState(anchorable).Export(stream);
                            }

                        }
                    }

                    using (IsolatedStorageFileStream stream = UserStore.OpenFile(@"state\mainwindow.xaml", FileMode.Create))
                    {
                        new MainWindowSavedState(this).Export(stream);
                    }

                    using (IsolatedStorageFileStream stream = UserStore.OpenFile(@"state\logwindow.xaml", FileMode.Create))
                    {
                        new WindowSavedState(EventLogWindow).Export(stream);
                    }

                    if(_graph != null)
                    {
                        using (IsolatedStorageFileStream stream = UserStore.OpenFile(@"graph.xaml", FileMode.Create))
                        {
                            _graph.Export(stream);
                        }
                    }

                    break;

                case CommandType.ABOUT:

                    Assembly assembly = Assembly.GetExecutingAssembly();
                    MessageBox.Show(String.Format("{0}\n{1}\nVersion : {2}",
                        assembly.GetCustomAttribute<AssemblyTitleAttribute>().Title,
                        assembly.GetCustomAttribute<AssemblyCopyrightAttribute>().Copyright,
                        assembly.GetCustomAttribute<AssemblyFileVersionAttribute>().Version
                    ), "About", MessageBoxButton.OK, MessageBoxImage.Information);

                    break;

                case CommandType.SET_CHART_TYPE:

                    VisualizationControl.InheritorInfo info = (VisualizationControl.InheritorInfo)parameter;
                    info.IsSelected = true;

                    Options.ChartType = info.Type;

                    Visualize(key);

                    break;

                case CommandType.NEXT:
                    this.Next();
                    break;

                case CommandType.PREVIOUS:
                    this.Previous();
                    break;

                case CommandType.REFRESH_TABLE:
                    this.OnReload((IReload)this.DataContext);
                    break;

                case CommandType.REFRESH_TREE:
                    this.OnReload(treeView);
                    break;

                case CommandType.EXIT:

                    if (MessageBox.Show("Do you want to save before exiting?", "Save", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                        Commands["Save"].Execute(null);
                                        
                    Environment.Exit(0);

                    break;
                    
            }

        }
        
        #endregion

        /// <summary>
        /// Creates a window in a new thread
        /// </summary>
        /// <param name="Configure">Delegate to run on the new thread after the window has been created.</param>
        public void Create(Action<VisualizationWindow> Configure = null)
        {

            VisualizationWindow w = null;

            // Creates a new blank window in a new thread
            Thread thread = new Thread(new ParameterizedThreadStart((s) =>
            {

                w = new VisualizationWindow()
                {
                    ControlsVisibility = Visibility.Visible
                };

                Thread.CurrentThread.Name = String.Format("{0} [{1}]", w.GetType().Name, w.Id);

                Dispatcher.Invoke(delegate
                {
                    _windows.Add(w);
                });

                w.Show();

                if (Configure != null)
                    Configure(w);

                System.Windows.Threading.Dispatcher.Run();

            }));

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            
        }

        #region Load/Save
        
        /// <summary>
        /// Reloads everything. 
        /// <see cref="IReload.Reload"/>
        /// </summary>
        public void Reload()
        {
                        
            if (!UserStore.DirectoryExists("windows"))
                UserStore.CreateDirectory("windows");

            if (!UserStore.DirectoryExists("state"))
                UserStore.CreateDirectory("state");

            treeView.Reload();

            _windows.Clear();

            // Load windows
            foreach (var filename in UserStore.GetFileNames(String.Format(WindowsFolderFormat, '*')))
            {

                using (IsolatedStorageFileStream stream = UserStore.OpenFile(String.Format(WindowsFolderFormat, filename), FileMode.Open, FileAccess.Read))
                {
                    Import(stream);
                }

            }

            // Load layout panes saved state
            foreach (object element in _layoutRoot.Descendents().ToArray())
            {

                if (element is LayoutAnchorable)
                {

                    LayoutAnchorable anchorable = (LayoutAnchorable)element;

                    try {

                        using (IsolatedStorageFileStream stream = UserStore.OpenFile(String.Format(@"state\{0}.xaml", anchorable.ContentId), FileMode.Open, FileAccess.Read))
                        {
                            ((LayoutAnchorableSavedState)XamlReader.Load(stream)).Restore(anchorable);
                        }

                    }
                    catch(FileNotFoundException) { }

                }
            }

            // Load mainwindow saved state
            try
            {

                using (IsolatedStorageFileStream stream = UserStore.OpenFile(@"state\mainwindow.xaml", FileMode.Open, FileAccess.Read))
                {
                    ((MainWindowSavedState)XamlReader.Load(stream)).Restore(this);
                }

            }
            catch (FileNotFoundException) {
                // Restore to defaults if file not found
                new MainWindowSavedState().Restore(this);
            }

            // Load logwindow saved state
            try
            {

                using (IsolatedStorageFileStream stream = UserStore.OpenFile(@"state\logwindow.xaml", FileMode.Open, FileAccess.Read))
                {
                    ((WindowSavedState)XamlReader.Load(stream)).Restore(EventLogWindow);
                }

            }
            catch (FileNotFoundException)
            {
                
            }

            try
            {

                using (IsolatedStorageFileStream stream = UserStore.OpenFile(@"graph.xaml", FileMode.Open, FileAccess.Read))
                {
                    _graph = ((VisualizationControl)XamlReader.Load(stream));
                }

            }
            catch (FileNotFoundException)
            {

            }

        }

        /// <summary>
        /// Imports a window into a new thread from the provided stream that contains the XAML to deserialize
        /// </summary>
        /// <param name="stream">The serialized text stream containing the XAML to deserialize</param>
        private static void Import(Stream stream)
        {
            
            MainWindow mainWindow = (MainWindow)App.Current.MainWindow;
            Dispatcher d = mainWindow.Dispatcher;
                        
            using (StreamReader reader = new StreamReader(stream))
            {

                Thread thread = new Thread(new ParameterizedThreadStart((s) => {

                        try {

                            VisualizationWindow w = (VisualizationWindow)XamlReader.Parse((string)s);
                            Guid Id = w.Id;

                            Thread.CurrentThread.Name = String.Format("{0} [{1}]", w.GetType().Name, Id);

                            d.InvokeAsync(delegate
                            {

                                if ((new List<Window>(mainWindow._windows)).Find((v) => { return ((VisualizationWindow)v).Id == Id; }) != null)
                                {
                                    w.Dispatcher.Invoke(delegate
                                    {
                                        w.Id = Guid.NewGuid();
                                    });

                                    MessageBox.Show(String.Format("Id was changed to {0}, because there was already a window with the same Id", Id), "Duplicate Window ID", MessageBoxButton.OK, MessageBoxImage.Information);
                                }

                                mainWindow.OnReload(w);
                                mainWindow._windows.Add(w);

                            });

                        }
                        catch(Exception e)
                        {
                            e.Show();
                            return;
                        }

                        System.Windows.Threading.Dispatcher.Run();

                    }));

                thread.SetApartmentState(ApartmentState.STA);
                thread.Start(reader.ReadToEnd());
                    

            }
            
        }
        

        /// <summary>
        /// Remove a window
        /// </summary>
        /// <param name="window">The window to remove</param>
        /// <param name="delete">If set to false, will not remove the saved file from the filesystem if any.</param>
        public void Remove(VisualizationWindow window, bool delete = true)
        {

            window.Dispatcher.InvokeAsync(delegate
            {
                window.Close();
                window.Dispose();

            });

            _windows.Remove(window);            

            if (delete)
            {
                try
                {
                    UserStore.DeleteFile(String.Format(WindowsFolderFormat, String.Format("{0}.xaml", window.Id)));
                }
                catch (IsolatedStorageException) { }
            }


        }

        #endregion
                

        /// <summary>
        /// Adds a chart to visualize the current data in the window
        /// </summary>
        /// <param name="key"></param>
        private void Visualize(KeyItem key)
        {

            MultiControl context = (MultiControl)this.DataContext;
            Type ChartType       = Options.ChartType;

            if (ChartType != null && key != null && key.Type != null)
            {

                if (_graph != null && !_graph.GetType().Equals(ChartType))
                {
                    _graph.Dispose();
                    _graph = null;                    
                }

                _graph = _graph ?? (VisualizationControl)ChartType.New();

                visualizationGrid.Children.Clear();

                if (_graph != null)
                {

                    visualizationGrid.Children.Add(_graph);

                    _graph.Owner = this;
                    _graph.Title = key.Parent.Path;

                    foreach (string path in _graph.Keys.ToArray())
                    {
                        _graph.Remove(KeyItem.CreateFromPath(path));
                    }

                    _graph.Add(key, context.Entries);
                    
                    string icon = (string)_graph.GetType().GetTypeInfo().GetCustomAttribute<IconAttribute>();                    
                    using (Stream stream = Application.GetResourceStream(new Uri(icon, UriKind.Relative)).Stream)
                    {
                        chartPropertiesButtonImage.Source = BitmapFrame.Create(stream);
                    }

                }
            }
        }

        #region Event Handlers     

        /// <summary>
        /// Used so that the items are checked accordingly in the context menu.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChartTypeMenuItem_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {

            KeyItem key = ((MultiControl)this.DataContext).Key;
            Type ChartType = Options.ChartType;

            if (key == null) return;

            foreach (VisualizationControl.InheritorInfo info in ((MenuItem)sender).Items)
            {
                info.IsSelected = ChartType != null && ChartType.Equals(info.Type);
            }

        }


        /// <summary>
        /// Used Support drag and dropping of exported files onto the mainwindow.
        /// </summary>
        /// <param name="sender">The sending <see cref="Window"/></param>
        /// <param name="e">The drag arguments</param>
        private void window_Drop(object sender, DragEventArgs e)
        {

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] paths = (string[])e.Data.GetData(DataFormats.FileDrop);

                foreach (string path in paths)
                {
                    using (FileStream stream = new FileStream(path, FileMode.Open))
                    {
                        try
                        {
                            Import(stream);
                        }
                        catch (Exception error)
                        {
                            MessageBox.Show(error.Message, "Import error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Used to disable the contextmenu items if nothing is selected in the tree
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="parameter"></param>
        /// <returns><c>true</c> if it can execute, <c>false</c> if not.. </returns>
        private bool ContextMenu_CanExecute(object sender, object parameter)
        {
            try
            {
                return treeView.Key != null;
            }
            catch (Exception) { }

            return false;
        }

        /// <summary>
        /// Changes the editor for properties that has value range data.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void settings_propertyGrid_SelectedObjectChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            
            PropertyGrid grid = (PropertyGrid)sender;
            Dictionary<PropertyDescriptor, PropertyItem> items = new Dictionary<PropertyDescriptor, PropertyItem>();
            IOptions settings = Options.Store;

            foreach (PropertyItem item in grid.Properties)
            {
                items.Add(item.PropertyDescriptor, item);
            }

            foreach (var p in settings.Get())
            {
                
                PropertyDescriptor descriptor = p.Key;

                PropertyItem item = items[descriptor];

                descriptor.RemoveValueChanged(settings, settings_propertyGrid_PropertyDescriptorValueChanged);

                if(descriptor.Name == "Store")
                {
                    descriptor.AddValueChanged(settings, delegate {
                        MessageBox.Show("Application must be restarted for this change to take effect", "Restart required", MessageBoxButton.OK, MessageBoxImage.Information);
                        Commands["Exit"].Execute(null);
                    });
                }
                else
                    descriptor.AddValueChanged(settings, settings_propertyGrid_PropertyDescriptorValueChanged);
                
                if (p.Value.Count > 0)
                {
                    StoreOptionEditor editor = new StoreOptionEditor(descriptor);
                    item.Editor = editor.ResolveEditor(item);
                }


            }

            grid.Update();
        }

        /// <summary>
        /// Refreshes the <see cref="settings_propertyGrid"/> editors.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void settings_propertyGrid_PropertyDescriptorValueChanged(object sender, EventArgs e)
        {
            settings_propertyGrid_SelectedObjectChanged(settings_propertyGrid, null);
        }

        private void settings_propertyGrid_PropertyValueChanged(object sender, PropertyValueChangedEventArgs e)
        {

            PropertyItem item = (PropertyItem)e.OriginalSource;
            PropertyGrid grid = (PropertyGrid)sender;

            if (item.DisplayName == "IndexField")
            {
                foreach(PropertyItem p in grid.Properties)
                {
                    if(p.DisplayName == "StartIndex" || p.DisplayName == "EndIndex")
                    {
                        if(p.Value is DateTime)
                        {
                            p.Editor = new Xceed.Wpf.Toolkit.PropertyGrid.Editors.DateTimeUpDownEditor().ResolveEditor(p);
                        }
                        else
                        {
                            p.Editor = new Xceed.Wpf.Toolkit.PropertyGrid.Editors.LongUpDownEditor().ResolveEditor(p);
                        }
                    }
                }

                ((PropertyGrid)sender).Update();

            }
        }

        #endregion

        /// <summary>
        /// Move to next results
        /// </summary>
        /// <returns><c>true</c> if there is more data, <c>false</c> if not</returns>
        public bool Next()
        {
            MultiControl control = ((MultiControl)this.DataContext);
            bool result = control.Next();
            RefreshSettingsValues(control);
            return result;
        }

        /// <summary>
        /// Move to previous results
        /// </summary>
        /// <returns><c>true</c> if there is more data, <c>false</c> if not</returns>
        public bool Previous()
        {
            MultiControl control = ((MultiControl)this.DataContext);
            bool result = control.Previous();
            RefreshSettingsValues(control);
            return result;
        }

        /// <summary>
        /// Refresh some values in the property grid for the <see cref="Options"/>
        /// </summary>
        private void RefreshSettingsValues(MultiControl control)
        {

            Options.StartIndex = Options.StartIndex;
            Options.EndIndex = Options.EndIndex;

            RefreshGraph();
                                    
        }

        /// <summary>
        /// Reloads an object
        /// </summary>
        /// <param name="obj">The object to reload</param>
        void OnReload(IReload obj)
        {
            if (obj == DataContext)
                RefreshGraph();

            Extensions.OnReload(this, obj);
        }

        /// <summary>
        /// Refreshes the graph when it has multiple keys in the main window
        /// </summary>
        private void RefreshGraph()
        {

            if (_graph != null)
            {

                MultiControl context = (MultiControl)DataContext;

                foreach (MultiControl c in _graph.Controls)
                {
                    if (c.Entries == context.Entries)
                        continue;

                    c.Get((Enum)Enum.Parse(PSMonitor.PSM.Store(Dispatcher).Index, Options.IndexField), Options.StartIndex, context.Get((Enum)Enum.Parse(PSMonitor.PSM.Store(Dispatcher).Index, Options.IndexField)).Count).Reload();

                }


            }
        }
        
    }
}
