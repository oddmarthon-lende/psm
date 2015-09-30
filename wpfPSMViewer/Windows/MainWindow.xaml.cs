/// <copyright file="mainwindow.xaml.cs" company="Baker Hughes Incorporated">
/// Copyright (c) 2015 All Rights Reserved
/// </copyright>
/// <author>Odd Marthon Lende</author>
/// <summary>Code behind for the Main Window</summary>

using PSMViewer.Properties;
using PSMViewer.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Collections.ObjectModel;
using PSMViewer.Models;
using PSMViewer.Visualizations;
using System.Windows.Markup;
using System.IO;
using System.IO.IsolatedStorage;
using System.Windows.Threading;
using Microsoft.Win32;
using System.Linq;
using System.Collections.Specialized;
using Xceed.Wpf.AvalonDock.Layout;
using System.Threading;
using System.Windows.Data;
using PSMViewer.Utilities;
using Xceed.Wpf.Toolkit.PropertyGrid;
using PSMonitor.Stores;
using PSMViewer.Editors;

namespace PSMViewer
{


    public sealed partial class MainWindow : Window, INotifyPropertyChanged, IReload
    {
        
        
        /// <summary>
        /// <see cref="IReload.Cancel"/>
        /// </summary>
        public CancellationTokenSource Cancel { get; private set; } = new CancellationTokenSource();

        #region Static Properties and Methods

        /// <summary>
        /// The default extension in file dialogs
        /// </summary>
        public static string DefaultExt = ".xaml";
        
        /// <summary>
        /// The default filter in file dialogs
        /// </summary>
        public static string Filter = "XAML documents (.xaml)|*.xaml";

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



        /// <summary>
        /// The type of chart to display in the mainwindow
        /// </summary>
        public Type ChartType
        {
            get { return (Type)GetValue(ChartTypeProperty); }
            set { SetValue(ChartTypeProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="ChartType"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ChartTypeProperty =
            DependencyProperty.Register("ChartType", typeof(Type), typeof(MainWindow), new PropertyMetadata(null));



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
        /// Gets the store options object
        /// </summary>
        public IOptions Settings_
        {
            get
            {
                return PSMonitor.PSM.Store(Dispatcher).Options;
            }
        }
               

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
            STOP
        }
               

        /// <summary>
        /// The MainWindow Constructor
        /// </summary>
        public MainWindow()
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

            InitializeComponent();

            Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            NameScope.SetNameScope(treeContextMenu, NameScope.GetNameScope(this));

            this.Loaded += delegate { this.OnReload(this); };
            this.Closing += (sender, e) =>
            {
                Commands["Exit"].Execute(null);
            };

            ((Main)DataContext).Timebased.Load += this.OnReload;
            ((Main)DataContext).Indexbased.Load += this.OnReload;

            ((Main)DataContext).Indexbased.Activate(this);

            ((Main)DataContext).PropertyChanged += (sender, e) =>
            {
                if(e.PropertyName == "Status")
                    Status = ((IReload)sender).Status;
            };

            _windows.CollectionChanged += delegate
            {
                OnPropertyChanged("Windows");
            };

            // Bind to the treeView Key property and update the data context
            treeView.SetBinding(Tree.KeyProperty, new Binding("Value")
            {
                Source = new BindingWrapper<KeyItem>(

                    key =>
                    {

                        Main context = (Main)this.DataContext;

                        if (key == null || key.Type == null) return key;

                        context.Selected = key;

                        Visualize(key);
                        this.OnReload(context);

                        return key;

                    }),
                Mode = BindingMode.OneWayToSource
            });          

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
            Main ViewModel = (Main)this.DataContext;
            KeyItem key = (KeyItem)treeView.SelectedValue;
            Window window = null;

            switch ((CommandType)cmd.Arguments[0].Value)
            {
                
                case CommandType.STOP:

                    ViewModel.Stop();
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
                        new WindowSavedState(this).Export(stream);
                    }

                    Settings.Default.Save();

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

                    ChartType = info.Type;

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
                    ((WindowSavedState)XamlReader.Load(stream)).Restore(this);
                }

            }
            catch (FileNotFoundException) {
                // Restore to defaults if file not found
                new WindowSavedState().Restore(this);
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

            Main context = (Main)this.DataContext;

            if (ChartType != null && key != null && key.Type != null)
            {

                VisualizationControl instance = (VisualizationControl)ChartType.New();
                instance.ContextMenu = null;

                foreach (VisualizationControl v in visualizationGrid.Children)
                {
                    v.Dispose();
                }

                visualizationGrid.Children.Clear();

                if (instance != null)
                {

                    instance.Owner = this;
                    instance.Title = key.Parent.Path;
                    instance.Add(key, context.Entries);

                    visualizationGrid.Children.Add(instance);                   

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

            KeyItem key = ((Main)this.DataContext).Selected;

            if (key == null) return;

            foreach (VisualizationControl.InheritorInfo info in ((MenuItem)sender).Items)
            {
                info.IsSelected = ChartType != null && key != null && ChartType.Equals(info.Type);
            }

        }

        /// <summary>
        /// Used to switch controls (Time, Indexed) when the mouse moves over the toolbars. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ToolBarTray_PreviewMouseMove(object sender, MouseEventArgs e)
        {

            ToolBarTray tray = (ToolBarTray)sender;

            foreach (ToolBar toolbar in tray.ToolBars)
            {

                if (VisualTreeHelper.GetDescendantBounds(toolbar).Contains(e.GetPosition(toolbar)))
                {
                    ((Controls)toolbar.DataContext).Activate(this);
                }


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
            IOptions settings = Settings_;

            foreach (PropertyItem item in grid.Properties)
                items.Add(item.PropertyDescriptor, item);

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
                    StoreOptionsEditor editor = new StoreOptionsEditor(descriptor);
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
        
        #endregion

        /// <summary>
        /// Move to next results
        /// </summary>
        /// <returns><c>true</c> if there is more data, <c>false</c> if not</returns>
        public bool Next()
        {
            return ((Main)this.DataContext).Next();
        }

        /// <summary>
        /// Move to previous results
        /// </summary>
        /// <returns><c>true</c> if there is more data, <c>false</c> if not</returns>
        public bool Previous()
        {
            return ((Main)this.DataContext).Previous();
        }
        
        
    }
}
