using PSMViewer.Properties;
using PSMViewer.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Globalization;
using System.Windows.Controls.DataVisualization.Charting;
using PSMonitor;
using System.Collections.ObjectModel;

namespace PSMViewer
{

    public enum Status
    {
        Loading,
        Error,
        Idle
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        
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
        public Status Status {
            get { return _status; }
            private set { SetField(ref _status, value); }
        }

        private ObservableCollection<Window> _windows = new ObservableCollection<Window>();
        public ObservableCollection<Window> Windows
        {
            get {
                return _windows;
            }
            set { _windows = value; }
        }

        public MainWindow()
        {           
            
            InitializeComponent();

            AddHandler(TreeViewItem.ExpandedEvent, new RoutedEventHandler(Reload));
            AddHandler(TreeViewItem.MouseDoubleClickEvent, new RoutedEventHandler(Reload));

            Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
            Application.Current.Exit += delegate { Exit(null, null); };

            NameScope.SetNameScope(treeContextMenu, NameScope.GetNameScope(this));

            RefreshTree(null, null);
           
        }

        private KeyItem GetDataContext(RoutedEventArgs e)
        {

            FrameworkElement item = null;
            KeyItem key = null;

            try {
                item = (FrameworkElement)e.OriginalSource;
            }
            catch(Exception) { }

            if (item == null) return null;

            try {
                key = (KeyItem)item.DataContext;
            }
            catch(Exception) { }

            if (key == null) return null;

            return key;
        }
        
        private void Reload(object sender, RoutedEventArgs e)
        {
            
            KeyItem key = GetDataContext(e);

            if (key == null) return;
            else if(e != null)
            {
                switch(e.RoutedEvent.Name)
                {

                    case "Expanded":

                        Reload(key);
                        break;

                    case "MouseDoubleClick":

                        Main context = (Main)this.DataContext;

                        if (key.Type == null) return;
                        
                        context.Selected = key;
                        Reload(context);

                        dock.IsEnabled = true;

                        return;

                }
            }
        }
        
        private void Reload(IReload obj)
        {

            if (obj == null) return;

            Status = Status.Loading;

            Dispatcher.InvokeAsync(obj.Reload).Task.ContinueWith(task =>
            {

                switch(task.Status)
                {

                    case TaskStatus.Faulted:

                        Status = Status.Error;

                        MessageBox.Show(String.Format("{0}\n{1}", task.Exception.Message, String.Join("\n", task.Exception.InnerExceptions.Select(e => { return e.Message; }).ToArray<string>()), "Something went wrong", MessageBoxButton.OK, MessageBoxImage.Error));

                        break;
                }

                Status = Status.Idle;

            });

        }

        private void CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void Next(object sender, ExecutedRoutedEventArgs e)
        {
            ((Controls)((FrameworkElement)sender).DataContext).Next();
        }

        private void Previous(object sender, ExecutedRoutedEventArgs e)
        {
            ((Controls)((FrameworkElement)sender).DataContext).Previous();
        }

        private void RefreshTable(object sender, ExecutedRoutedEventArgs e)
        {
            Reload(((Controls)((FrameworkElement)sender).DataContext));
        }

        private void RefreshTree(object sender, ExecutedRoutedEventArgs e)
        {
            Reload((KeyItem)rootItem.DataContext);
        }

        private void Exit(object sender, ExecutedRoutedEventArgs e)
        {
            Settings.Default.Save();
            Environment.Exit(0);
        }

        private void About(object sender, ExecutedRoutedEventArgs e)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            MessageBox.Show(String.Format("{0}\n{1}\nVersion : {2}",
                assembly.GetCustomAttribute<AssemblyTitleAttribute>().Title,
                assembly.GetCustomAttribute<AssemblyCopyrightAttribute>().Copyright,
                assembly.GetCustomAttribute<AssemblyFileVersionAttribute>().Version
            ), "About", MessageBoxButton.OK, MessageBoxImage.Information);

        }
        private void ChangeChartType(object sender, ExecutedRoutedEventArgs e)
        {
            KeyItem key = (KeyItem)((FrameworkElement)e.OriginalSource).DataContext;
        }

        private void RemoveChart(object sender, ExecutedRoutedEventArgs e)
        {
            KeyItem key = (KeyItem)((FrameworkElement)e.OriginalSource).DataContext;
        }

        private void AddChart(object sender, ExecutedRoutedEventArgs e)
        {

            KeyItem key = (KeyItem)((FrameworkElement)e.OriginalSource).DataContext;

            if (key != null && key.Type != null)
            {

                DataPointSeries chart = (DataPointSeries)Activator.CreateInstance(key.ChartType);
                
                chart.SetBinding(DataPointSeries.DataContextProperty, new Binding("DataContext") { Source = this });
                chart.SetBinding(DataPointSeries.TitleProperty, new Binding("Name") { Source = key });
                
                chart.ItemsSource = ((Main)this.DataContext).Reload(key);

                chart.DependentValuePath = "Value";
                chart.IndependentValuePath = "Timestamp";

                if (key.Chart != null)
                {

                    chartHost.Series.Remove(key.Chart);
                    chartHost.UpdateLayout();
                   
                }

                key.Chart = chart;
                
                chartHost.Series.Add(chart);
                chartHost.UpdateLayout();

            }
        }

        private void Reload(object sender, KeyEventArgs e)
        {
            RefreshTable(sender, null);
        }

        private void ContextMenuClick(object sender, ExecutedRoutedEventArgs e)
        {
            MenuItem item = (MenuItem) e.OriginalSource;
            
            switch((string)item.Header)
            {
                case "New Window" :
                    Windows.Add(new ChartWindow() { Title = "Hello" });
                    break;
                case "Existing" :                    
                    break;
                default:
                    break;
            }

        }
    }
}
