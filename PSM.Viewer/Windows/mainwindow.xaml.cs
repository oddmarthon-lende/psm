﻿/// <copyright file="mainwindow.xaml.cs" company="Baker Hughes Incorporated">
/// Copyright (c) 2015 All Rights Reserved
/// </copyright>
/// <author>Odd Marthon Lende</author>
/// <summary>Code behind for the Main Window</summary>

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using PSM.Viewer.Models;
using System.Windows.Markup;
using System.IO;
using System.IO.IsolatedStorage;
using System.Windows.Threading;
using Microsoft.Win32;
using System.Linq;
using Xceed.Wpf.AvalonDock.Layout;
using System.Threading;
using System.Windows.Data;
using PSM.Viewer.Utilities;
using Xceed.Wpf.Toolkit.PropertyGrid;
using PSM.Viewer.Commands;
using System.Diagnostics;
using Xceed.Wpf.DataGrid.Converters;

namespace PSM.Viewer
{

    public partial class MainWindow : Theme.Window, INotifyPropertyChanged, IReload, IDisposable
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
        internal static string WindowsFolderFormat = @"windows\{0}";

        /// <summary>
        /// The isolated storage file store for this application.
        /// </summary>
        internal static IsolatedStorageFile UserStore = IsolatedStorageFile.GetUserStoreForDomain();
               

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


        private Settings _options;
        /// <summary>
        /// Gets the options object
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public Settings Options
        {
            get { return _options; }
            set {

                _options = value;
                _options.Window = this;

                propertyGrid_top.SelectedObject = _options;
                propertyGrid_bottom.SelectedObject = _options.Store;

            }
        }

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
            /// Delete key from tree
            /// </summary>
            DELETE,
            RESET
        }

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

                for (int i = 0; i < vb.Length; i++)
                {
                    tbs[i].Visibility = vb[i];
                }

            }
        }

        /// <summary>
        /// The MainWindow Constructor
        /// </summary>
        public MainWindow()
        {
            CreateCommands();
            InitializeComponent();

            Options = new Settings();

            Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            NameScope.SetNameScope(treeContextMenu, NameScope.GetNameScope(this));

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

                            Title = key.ToString();

                            if (key.Type == null)
                                return key;

                            ctrl.Key = key;
                            
                            this.OnReload(ctrl);

                        }                        

                        return key;

                    }),

                Mode = BindingMode.OneWayToSource

            });

            treeView.KeyRightClick += TreeView_KeyRightClick;

            EventLogWindow = new EventLogWindow() { };
            EventLogWindow.Closing += EvtWindow_Closing;

            SetupIndexColumns();
        }
               
        /// <summary>
        /// Generates columns for the data indexes
        /// </summary>
        private void SetupIndexColumns()
        {

            StringFormatConverter converter = new StringFormatConverter();
            string[] names = Enum.GetNames(PSM.Store.Get(Dispatcher.CurrentDispatcher).Index);

            dataGrid.Columns.Clear();

            dataGrid.Columns.Add(new DataGridTextColumn()
            {
                Header = "Value",
                Width = new DataGridLength(50, DataGridLengthUnitType.Star),
                IsReadOnly = true,
                Binding = new Binding("Value")
                {
                    Converter = converter
                }
            });            

            foreach(string name in names)
            {

                dataGrid.Columns.Add(new DataGridTextColumn()
                {
                    Header = String.Format("IDX:{0}", name),
                    Width = new DataGridLength((int)(50 / names.Length), DataGridLengthUnitType.Star),
                    IsReadOnly = true,
                    Binding = new Binding(String.Format("Index[{0}]", name))
                    {
                        Converter = converter
                    }

                });

            }
                        

        }

        /// <summary>
        /// 
        /// </summary>
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
            Commands.Add("Reset", new RelayCommand(ExecuteCommand, delegate {
                return DataContext == null ? false : ((MultiControl)DataContext).Get().Page > 0;
            }, CommandType.RESET));
            Commands.Add("Next", new RelayCommand(ExecuteCommand, canExecute, CommandType.NEXT));
            Commands.Add("Previous", new RelayCommand(ExecuteCommand, delegate {
                return DataContext == null ? false : ((MultiControl)DataContext).Get().Page > 0;
            }, CommandType.PREVIOUS));
            Commands.Add("NewWindow", new RelayCommand(ExecuteCommand, canExecute, CommandType.NEW_WINDOW));
            Commands.Add("Stop", new RelayCommand(ExecuteCommand, canExecute, CommandType.STOP));
            Commands.Add("EventLog", new RelayCommand(ExecuteCommand, canExecute, CommandType.EVENT_LOG));
            Commands.Add("DeleteFromTree", new RelayCommand(ExecuteCommand, canExecute, CommandType.DELETE));
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="key"></param>
        /// <param name="args"></param>
        private void TreeView_KeyRightClick(Tree sender, KeyItem key, System.Windows.Input.MouseEventArgs args)
        {

            ContextMenu menu = new ContextMenu();
            MenuItem item;
            
            item = new MenuItem();

            item.Command = Commands["DeleteFromTree"];
            item.CommandParameter = key;
            item.Header = "Delete";

            menu.Items.Add(item);

            menu.IsOpen = true;

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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
            KeyItem key = treeView.Key;
            Window window = null;

            switch ((CommandType)cmd.Arguments[0].Value)
            {

                case CommandType.DELETE:

                    key = (KeyItem)parameter;

                    try
                    {
                        if(MessageBox.Show(String.Format("Do you want to delete the key:\n {0}", key.Path), "Delete", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                            if (MessageBox.Show("Are you sure?\nThis will delete all sub keys and data.", "Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                                Store.Get(Dispatcher.CurrentDispatcher).Delete(key.Path);

                        Commands["RefreshTree"].Execute(null);
                    }
                    catch(Exception e) {
                        MessageBox.Show(e.Message, "An Error Occurred", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    

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
                        RestoreDirectory = true,
                        Multiselect = true
                    };

                    if (dialog.ShowDialog().Value == true)
                    {
                        foreach(Stream s in dialog.OpenFiles())
                        {
                            Import(s);
                        }
                    }

                    break;

                case CommandType.SAVE:
                    
                    foreach (VisualizationWindow w in _windows)
                    {
                                                
                        using (IsolatedStorageFileStream stream = UserStore.OpenFile(String.Format(WindowsFolderFormat, String.Format("{0}.xaml", w.Id)), FileMode.Create))
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
                            
                            using (IsolatedStorageFileStream stream = UserStore.OpenFile(String.Format(@"state\{0}.xaml", anchorable.ContentId), FileMode.Create))
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
                                       

                    break;

                case CommandType.ABOUT:

                    Assembly assembly = Assembly.GetExecutingAssembly();
                    MessageBox.Show(String.Format("{0}\n{1}\nVersion {2}",
                        assembly.GetCustomAttribute<AssemblyTitleAttribute>().Title,
                        assembly.GetCustomAttribute<AssemblyCopyrightAttribute>().Copyright,
                        assembly.GetCustomAttribute<AssemblyFileVersionAttribute>().Version
                    ), "About", MessageBoxButton.OK, MessageBoxImage.Information);

                    break;
                    
                case CommandType.NEXT:
                    this.Next();
                    break;

                case CommandType.PREVIOUS:
                    this.Previous();
                    break;

                case CommandType.RESET:
                    this.Reset();
                    break;

                case CommandType.REFRESH_TABLE:
                    this.OnReload((IReload)this.DataContext);
                    break;

                case CommandType.REFRESH_TREE:
                    this.OnReload(treeView);
                    break;

                case CommandType.EXIT:

                    foreach (WindowInfo w in Windows)
                    {
                        VisualizationWindow v = (VisualizationWindow)w.Window;
                        v.Dispatcher.InvokeAsync(v.Dispose);
                    }
                        

                    this.Dispose();

                    Application.Current.Shutdown();

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

                w = new VisualizationWindow();

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
                    try
                    {
                        Import(stream);
                    }
                    catch(Exception e)
                    {
                        Logger.Error(e);
                    }
                    
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
                        

        }

        /// <summary>
        /// Imports a window into a new thread from the provided stream that contains the XAML to deserialize
        /// </summary>
        /// <param name="stream">The serialized text stream containing the XAML to deserialize</param>
        private static VisualizationWindow Import(Stream stream)
        {
            
            MainWindow mainWindow = (MainWindow)App.Current.MainWindow;
            Dispatcher d = mainWindow.Dispatcher;
            VisualizationWindow w = null;
            Exception error = null;

            using (StreamReader reader = new StreamReader(stream))
            {

                Thread thread = new Thread(new ParameterizedThreadStart((s) => {

                        try {

                            w = (VisualizationWindow)XamlReader.Parse((string)s);
                            Guid Id = w.Id;

                            Thread.CurrentThread.Name = String.Format("{0} [{1}]", w.GetType().Name, Id);                            

                            if ((new List<Window>(mainWindow._windows)).Find((v) => { return ((VisualizationWindow)v).Id == Id; }) != null)
                            {
                                w.Dispatcher.Invoke(delegate
                                {
                                    w.Id = Guid.NewGuid();
                                });
                            
                                MessageBox.Show(String.Format("Id was changed to {0}, because there was already a window with the same Id", Id), "Duplicate Window ID", MessageBoxButton.OK, MessageBoxImage.Information);
                            }

                            mainWindow._windows.Add(w);

                        }
                        catch(Exception e)
                        {
                            error = e;
                            return;
                        }

                        System.Windows.Threading.Dispatcher.Run();

                    }));

                thread.SetApartmentState(ApartmentState.STA);
                thread.Start(reader.ReadToEnd());

                while (w == null)
                {
                    if (error != null)
                    {
                        Debug.Fail(error.Message, error.StackTrace);
                        throw error;
                    }

                    Thread.Sleep(100);
                }

                return w;              

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
               
                
        #region Event Handlers     
        
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

            Options.settings_propertyGrid_SelectedObjectChanged(sender, e);
            SetupIndexColumns();
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void settings_propertyGrid_PropertyValueChanged(object sender, PropertyValueChangedEventArgs e)
        {
            
            Options.settings_propertyGrid_PropertyValueChanged(sender, e);    

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

        public void Reset()
        {
            MultiControl control = ((MultiControl)this.DataContext);
            control.Reset();
            RefreshSettingsValues(control);
        }

        /// <summary>
        /// Refresh some values in the property grid for the <see cref="Options"/>
        /// </summary>
        private void RefreshSettingsValues(MultiControl control)
        {

            Options.StartIndex = Options.StartIndex;
            Options.EndIndex = Options.EndIndex;
                        
        }

        /// <summary>
        /// Reloads an object
        /// </summary>
        /// <param name="obj">The object to reload</param>
        void OnReload(IReload obj)
        {
            Extensions.OnReload(this, obj);
        }

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            Store.Get().Dispose();
            Store.Get(Dispatcher.CurrentDispatcher).Dispose();
        }
    }
}
