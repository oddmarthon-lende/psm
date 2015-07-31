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

namespace PSMViewer
{
        
    public enum Status
    {
        Loading,
        Error,
        Idle
    }

    public partial class MainWindow : Window, INotifyPropertyChanged, IReload
    {

        public static string DefaultExt = ".xaml";
        public static string Filter = "XAML documents (.xaml)|*.xaml";

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

        private Status _status = Status.Idle;
        public Status Status
        {
            get { return _status; }
            private set { SetField(ref _status, value); }
        }

        public bool navigationEnabled
        {

            get
            {
                return ((Main)this.DataContext).Selected != null;
            }
        }

        private ObservableCollection<Window> _windows = new ObservableCollection<Window>();
        public ObservableCollection<Window> Windows
        {
            get
            {
                return _windows;
            }
            set { _windows = value; }
        }

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
            Commands.Add("ChartToNewWindow", new RelayCommand(ExecuteCommand, ContextMenu_CanExecute, CommandType.CHART_TO_NEW_WINDOW));
            Commands.Add("SetChartType", new RelayCommand(ExecuteCommand, ContextMenu_CanExecute, CommandType.SET_CHART_TYPE));
            Commands.Add("Next", new RelayCommand(ExecuteCommand, canExecute, CommandType.NEXT));
            Commands.Add("Previous", new RelayCommand(ExecuteCommand, canExecute, CommandType.PREVIOUS));
            Commands.Add("NewWindow", new RelayCommand(ExecuteCommand, canExecute, CommandType.NEW_WINDOW));

            InitializeComponent();

            AddHandler(TreeView.SelectedItemChangedEvent, new RoutedPropertyChangedEventHandler<object>(treeView_SelectedItemChanged));
            
            Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            NameScope.SetNameScope(treeContextMenu, NameScope.GetNameScope(this));

            this.Loaded += delegate { Reload(this); };
            this.Closing += (sender, e) =>
            {
                Commands["Exit"].Execute(null);
            };
        }

        #region Commands

        public CommandCollection Commands { get; private set; } = new CommandCollection();

        private void ExecuteCommand(object sender, object parameter)
        {

            RelayCommand cmd = (RelayCommand)sender;
            Main context = (Main)this.DataContext;
            KeyItem key = (KeyItem)treeView.SelectedValue;
            VisualizationWindow window = null;

            switch ((CommandType)cmd.Arguments[0].Value)
            {
                case CommandType.NEW_WINDOW:

                    Windows.Add(new VisualizationWindow() { ControlsVisibility = Visibility.Visible });
                    Windows.Last().Show();

                    break;

                case CommandType.WINDOWS:

                    window = (VisualizationWindow)parameter;

                    window.Show();
                    window.Focus();

                    Reload(window);

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

                    if (!store.DirectoryExists("windows"))
                        store.CreateDirectory("windows");

                    foreach (VisualizationWindow w in Windows)
                    {
                        using (IsolatedStorageFileStream stream = store.OpenFile(String.Format(pattern, String.Format("{0}.xaml", w.Id)), FileMode.Create))
                        {
                            Export(w, stream);
                        }
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

                case CommandType.CHART_TO_NEW_WINDOW:

                    VisualizationControl chart = (VisualizationControl)Activator.CreateInstance(((VisualizationControl.InheritorInfo)parameter).Type);
                    window = new VisualizationWindow(chart) { Title = key.Type == null ? key.Path : key.Parent.Path };

                    Windows.Add(window);
                    chart.Add(key);
                    window.Show();
                    Reload(window);

                    break;

                case CommandType.SET_CHART_TYPE:

                    VisualizationControl.InheritorInfo info = (VisualizationControl.InheritorInfo)parameter;
                    info.IsSelected = true;

                    VisualizationControl.SetChartType(key, info.Type);

                    Visualize(key);

                    break;

                case CommandType.NEXT:
                    this.Next();
                    break;

                case CommandType.PREVIOUS:
                    this.Previous();
                    break;

                case CommandType.REFRESH_TABLE:
                    Reload((IReload)this.DataContext);
                    break;

                case CommandType.REFRESH_TREE:
                    Reload(treeView);
                    break;

                case CommandType.EXIT:

                    if (MessageBox.Show("Do you want to save before exiting?", "Save", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                        Commands["Save"].Execute(null);

                    Environment.Exit(0);

                    break;

            }

        }

        private bool ContextMenu_CanExecute(object sender, object parameter)
        {
            try
            {
                return treeView.SelectedValue != null;
            }
            catch (Exception) { }

            return false;
        }

        #endregion

        #region Load/Save

        static string pattern = @"windows\{0}";
        static IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForDomain();

        public void Reload()
        {
            treeView.Reload();

            Windows.Clear();

            foreach (var filename in store.GetFileNames(String.Format(pattern, '*')))
            {

                using (IsolatedStorageFileStream stream = store.OpenFile(String.Format(pattern, filename), FileMode.Open, FileAccess.Read))
                {
                    Import(stream);
                }

            }           

        }

        public static void Import(Stream stream)
        {

            MainWindow mainWindow = (MainWindow)App.Current.MainWindow;
            VisualizationWindow w = (VisualizationWindow)XamlReader.Load(stream);

            if ((new List<Window>(mainWindow.Windows)).Find((v) => { return ((VisualizationWindow)v).Id == w.Id; }) != null)
            {
                w.Id = new Guid();
                MessageBox.Show(String.Format("Id was changed to {0}, because there was already a window with the same Id", w.Id), "Duplicate Window ID", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            mainWindow.Reload(w);
            mainWindow.Windows.Add(w);

        }

        public static void Export(VisualizationWindow window, Stream stream)
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

            window.Owner = null;

            XamlWriter.Save(window, mgr);

        }

        public void Remove(VisualizationWindow window, bool delete = true)
        {

            window.Close();
            Windows.Remove(window);

            if (delete)
            {
                try
                {
                    store.DeleteFile(String.Format(pattern, String.Format("{0}.xaml", window.Id)));
                }
                catch (IsolatedStorageException) { }
            }


        }

        #endregion

        public void Reload(IReload obj)
        {

            if (obj == null) return;

            Status = Status.Loading;

            Dispatcher.InvokeAsync(obj.Reload, DispatcherPriority.ApplicationIdle).Task.ContinueWith(task =>
            {

                switch (task.Status)
                {

                    case TaskStatus.Faulted:

                        Status = Status.Error;

                        MessageBox.Show(task.Exception.GetBaseException().Message, task.Exception.Message, MessageBoxButton.OK, MessageBoxImage.Error);

                        break;

                    default:
                        Status = Status.Idle;
                        break;
                }



            });

        }

        private void Visualize(KeyItem key)
        {

            Main context = (Main)this.DataContext;

            if (key != null && key.Type != null)
            {

                VisualizationControl instance = VisualizationControl.Restore(key);

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

        private void ChartTypeMenuItem_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {

            KeyItem key = ((Main)this.DataContext).Selected;

            if (key == null) return;

            foreach (VisualizationControl.InheritorInfo info in ((MenuItem)sender).Items)
            {
                Type t = VisualizationControl.GetChartType(key);
                info.IsSelected = t != null && key != null && t.Equals(info.Type);
            }

        }

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

        private void treeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            KeyItem key;
            
            try {
                key = (KeyItem)e.NewValue;
            }
            catch(Exception)
            {
                return;
            }
            
            Main context = (Main)this.DataContext;

            if (key == null || key.Type == null) return;

            context.Selected = key;
            
            Visualize(key);
            Reload(context);
        }

        #endregion

        public bool Next()
        {
            return ((Main)this.DataContext).Next();
        }

        public bool Previous()
        {
            return ((Main)this.DataContext).Previous();
        }

        private void window_Drop(object sender, DragEventArgs e)
        {
            
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] paths = (string[])e.Data.GetData(DataFormats.FileDrop);

                foreach(string path in paths)
                {
                    using (FileStream stream = new FileStream(path, FileMode.Open))
                    {
                        try
                        {
                            Import(stream);
                        }
                        catch(Exception error)
                        {
                            MessageBox.Show(error.Message, "Import error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
        }
    }
}
