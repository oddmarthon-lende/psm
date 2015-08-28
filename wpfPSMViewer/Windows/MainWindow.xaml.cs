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
using System.Threading.Tasks;
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
using System.Xml;
using System.Windows.Threading;
using Microsoft.Win32;
using System.Linq;
using System.Collections.Specialized;
using Xceed.Wpf.AvalonDock.Layout;
using System.Threading;
using System.Windows.Media.Imaging;
using System.Windows.Data;
using PSMViewer.Utilities;

namespace PSMViewer
{
        
    
    public partial class MainWindow : Window, INotifyPropertyChanged, IReload
    {

        
        private CancellationTokenSource _c = new CancellationTokenSource();
        public CancellationTokenSource Cancel
        {
            get
            {
                return _c;
            }
        }

        #region Static Properties and Methods

        public static string DefaultExt = ".xaml";
        public static string Filter = "XAML documents (.xaml)|*.xaml";

        static string WindowsFolderFormat = @"windows\{0}";
        static IsolatedStorageFile UserStore = IsolatedStorageFile.GetUserStoreForDomain();

        /// <summary>
        /// Set the chart type for the specified key
        /// </summary>
        /// <param name="key"></param>
        /// <param name="chartType"></param>
        /// <returns>The chart type</returns>
        private static Type SetChartType(KeyItem key, Type chartType)
        {

            Settings s = Settings.Default;

            foreach (string t in (from string t in s.chartType
                                  where t.StartsWith(key.Path)
                                  select t).ToArray())
            {
                s.chartType.Remove(t);
            }

            s.chartType.Add(String.Format("{0},{1}", key.Path, chartType.FullName));
            s.Save();

            return chartType;

        }

        /// <summary>
        /// Get the chart type for the specified key
        /// </summary>
        /// <param name="key"></param>
        /// <returns>The chart type for the key if found or <c>null</c> if not found</returns>
        private static Type GetChartType(KeyItem key)
        {

            Settings.Default.chartType = Settings.Default.chartType ?? new StringCollection();

            Type chartType = (from string t in Settings.Default.chartType
                              where (t.StartsWith(key.Path))
                              select Type.GetType(t.Split(',')[1])).ElementAtOrDefault(0);

            return chartType;

        }

        private static VisualizationControl Restore(KeyItem key)
        {
            Type t = GetChartType(key);
            return t == null ? null : (VisualizationControl)Activator.CreateInstance(t);
        }

        #endregion

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


        /// <summary>
        /// Used to show a status in the bottom of the window
        /// </summary>
        public ReloadStatus Status
        {
            get { return (ReloadStatus)GetValue(StatusProperty); }
            set { SetValue(StatusProperty, value); }
        }
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
        /// Used to read properties from the windows objects, because they were created in a different thread.        
        /// </summary>
        public class WindowInfo : DispatcherObjectPropertyWrapper
        {
            public VisualizationWindow Window { get; private set; }

            public string Title {

                get
                {
                    return (string)Window.GetValue("Title");
                }
            }

            public BitmapSource Thumbnail {

                get
                {
                    return (BitmapSource)Window.GetValue("Thumbnail");
                }
            }

            public WindowInfo(VisualizationWindow window) : base(window)
            {
                this.Window = window;
            }
        }

        /// <summary>
        /// Used to identify commands
        /// </summary>
        private enum CommandType
        {
            ABOUT,
            IMPORT,
            REFRESH_TABLE,
            REFRESH_TREE,
            NEXT,
            PREVIOUS,
            WINDOWS,
            EXIT,
            SAVE,
            CHART_TO_NEW_WINDOW,
            SET_CHART_TYPE,
            NEW_WINDOW
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
            Main context = (Main)this.DataContext;
            KeyItem key = (KeyItem)treeView.SelectedValue;
            VisualizationWindow window = null;

            switch ((CommandType)cmd.Arguments[0].Value)
            {

                // Creates a new blank window in a new thread
                case CommandType.NEW_WINDOW:

                    Thread thread = new Thread(new ParameterizedThreadStart((s) =>
                    {

                        VisualizationWindow w = new VisualizationWindow() {
                            ControlsVisibility = Visibility.Visible
                        };

                        Dispatcher.InvokeAsync(delegate
                        {
                            _windows.Add(w);
                            w.Dispatcher.InvokeAsync(w.Show);
                        });

                        System.Windows.Threading.Dispatcher.Run();

                    }));

                    thread.SetApartmentState(ApartmentState.STA);
                    thread.Start();

                    break;

                case CommandType.WINDOWS:

                    window = ((WindowInfo)parameter).Window;

                    window.Dispatcher.InvokeAsync(window.Focus);
                    this.OnReload(window);

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
                            Export(w, stream);
                        }
                    }

                    foreach (object element in _layoutRoot.Descendents())
                    {
                        if (element is LayoutAnchorable)
                        {
                            
                            LayoutAnchorable anchorable = (LayoutAnchorable)element;
                            
                            using (IsolatedStorageFileStream stream = store.OpenFile(String.Format(@"state\{0}.xaml", anchorable.ContentId), FileMode.Create))
                            {
                                Export(new LayoutAnchorableSavedState(anchorable), stream);
                            }

                        }
                    }

                    using (IsolatedStorageFileStream stream = UserStore.OpenFile(@"state\mainwindow.xaml", FileMode.Create))
                    {
                        Export(new WindowSavedState(this), stream);
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

                    SetChartType(key, info.Type);

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
        /// Import a window into a new thread from the provided stream
        /// </summary>
        /// <param name="stream">The serialized text stream containing the XAML to de-serialize</param>
        private static void Import(Stream stream)
        {

            MainWindow mainWindow = (MainWindow)App.Current.MainWindow;
            Dispatcher d = mainWindow.Dispatcher;

            using (StreamReader reader = new StreamReader(stream))
            {

                Thread thread = new Thread(new ParameterizedThreadStart((s) => {

                        VisualizationWindow w = (VisualizationWindow)XamlReader.Parse((string)s);
                        Guid Id = w.Id;

                        Thread.CurrentThread.Name = String.Format("{0} [{1}]", w.GetType(), Id);

                        d.InvokeAsync(delegate
                        {

                            if ((new List<Window>(mainWindow._windows)).Find((v) => { return ((VisualizationWindow)v).Id == Id; }) != null)

                            {
                                w.Dispatcher.Invoke(delegate
                                {
                                    w.Id = new Guid();
                                });

                                MessageBox.Show(String.Format("Id was changed to {0}, because there was already a window with the same Id", Id), "Duplicate Window ID", MessageBoxButton.OK, MessageBoxImage.Information);
                            }

                            mainWindow.OnReload(w);
                            mainWindow._windows.Add(w);

                        });

                        System.Windows.Threading.Dispatcher.Run();

                    }));

                thread.SetApartmentState(ApartmentState.STA);
                thread.Start(reader.ReadToEnd());

            }
            
        }
                
        /// <summary>
        /// Serializes objects to a stream
        /// </summary>
        /// <param name="obj">The object to serialize</param>
        /// <param name="stream">The stream that the XAML is written to</param>
        public static void Export(object obj, Stream stream)
        {

            XmlWriter writer = XmlWriter.Create(stream, new XmlWriterSettings
            {
                Indent = true,
                ConformanceLevel = ConformanceLevel.Auto,
                OmitXmlDeclaration = true

            });

            XamlDesignerSerializationManager mgr = new XamlDesignerSerializationManager(writer)
            {
                XamlWriterMode = XamlWriterMode.Expression
            };
            
            XamlWriter.Save(obj, mgr);

        }

        /// <summary>
        /// Remove a window
        /// </summary>
        /// <param name="window">The window to remove</param>
        /// <param name="delete">If set to false, will not remove the saved file from the filesystem if any.</param>
        public void Remove(VisualizationWindow window, bool delete = true)
        {

            window.Dispatcher.InvokeAsync(window.Close);

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

            if (key != null && key.Type != null)
            {

                VisualizationControl instance = Restore(key);

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
                Type t = GetChartType(key);
                info.IsSelected = t != null && key != null && t.Equals(info.Type);
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
                    ((Controls)toolbar.DataContext).Activate();
                }


            }
        }


        /// <summary>
        /// Used Support drag and dropping of exported files onto the mainwindow.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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
        /// <returns><c>True</c> if it can execute, <c>False</c> if not.. </returns>
        private bool ContextMenu_CanExecute(object sender, object parameter)
        {
            try
            {
                return treeView.Key != null;
            }
            catch (Exception) { }

            return false;
        }

        #endregion

        /// <summary>
        /// Move to next results
        /// </summary>
        /// <returns><c>True</c> if there is more data, <c>False</c> if not</returns>
        public bool Next()
        {
            return ((Main)this.DataContext).Next();
        }

        /// <summary>
        /// Move to previous results
        /// </summary>
        /// <returns><c>True</c> if there is more data, <c>False</c> if not</returns>
        public bool Previous()
        {
            return ((Main)this.DataContext).Previous();
        }
                
    }
}
