using PSMViewer.Properties;
using PSMViewer.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Collections.ObjectModel;
using PSMViewer.Models;
using PSMViewer.Visualizations;
using System.Collections.Specialized;

namespace PSMViewer
{

    static class Extensions
    {

        public static T GetTemplatedParent<T>(this FrameworkElement o)
        where T : DependencyObject
        {
            DependencyObject child = o, parent = null;

            while (child != null && (parent = LogicalTreeHelper.GetParent(child)) == null)
            {
                child = VisualTreeHelper.GetParent(child);
            }

            FrameworkElement frameworkParent = parent as FrameworkElement;

            return frameworkParent != null ? frameworkParent.TemplatedParent as T : null;
        }
    }

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
            catch (Exception) { }

            if (item == null) return null;

            try {
                key = (KeyItem)item.DataContext;
            }
            catch (Exception) { }

            if (key == null) return null;

            return key;
        }

        private void Reload(object sender, RoutedEventArgs e)
        {

            KeyItem key = GetDataContext(e);

            if (e != null)
            {
                switch (e.RoutedEvent.Name)
                {

                    case "Expanded":

                        Reload(key);
                        break;

                    case "MouseDoubleClick":

                        Main context = (Main)this.DataContext;

                        if (key == null || key.Type == null) return;

                        context.Selected = key;

                        Reload(context);

                        dock.IsEnabled = true;

                        Visualize(key);

                        break;

                    default:
                        RefreshTable(sender, null);
                        break;

                }
            }
        }

        private void Reload(IReload obj)
        {

            if (obj == null) return;

            Status = Status.Loading;

            Dispatcher.InvokeAsync(obj.Reload).Task.ContinueWith(task =>
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
        
        private void Reload(object sender, KeyEventArgs e)
        {
            RefreshTable(sender, null);
        }       

        private void ContextMenuClick(object sender, ExecutedRoutedEventArgs e)
        {
            
            Main context  = (Main)this.DataContext;
            MenuItem item = (MenuItem) e.OriginalSource;
            VisualizationWindow cw = null;
            KeyItem key = (KeyItem)treeView.SelectedValue;

            switch ( ((HeaderedItemsControl)(item.Parent??e.Source)).Header.ToString() )
            {

                case "To Existing Window":

                    cw = (from VisualizationWindow w in Windows where w.Title == (string)item.Header select w).ElementAtOrDefault(0);
                    cw.Add(context.Selected.Parent.Path).Add(key);

                    break;

                case "Chart Type":

                    switch ((string)item.Header)
                    {
                        default:
                            VisualizationControl.SetChartType(key, ((VisualizationControl.InheritorInfo)item.DataContext).Type);
                            break;
                    }

                    foreach(VisualizationControl.InheritorInfo info in ((MenuItem)(item.Parent ?? e.Source)).Items)
                    {
                        info.IsSelected = info == item.DataContext;
                    }
                    
                    Visualize(key);

                    return;

                default:
                    break;
            }

            switch( (string) item.Header )
            {

                case "To New Window" :
                    
                    Windows.Add(new VisualizationWindow() { Title = Guid.NewGuid().ToString() });

                    ( (VisualizationWindow) Windows.Last() ).Add(key.Type == null ? key.Path : key.Parent.Path).Add(key);

                    Windows.Last().Show();
                    Windows.Last().Closed += ChartWindow_Closed;

                    break;

                default:
                    break;
            }

        }

        private void Visualize(KeyItem key)
        {

            Main context = (Main)this.DataContext;

            if (key != null && key.Type != null)
            {

                VisualizationControl instance = VisualizationControl.Restore(key);
                
                foreach(VisualizationControl v in visualizationGrid.Children)
                {
                    v.Dispose();
                }

                visualizationGrid.Children.Clear();

                if (instance != null)
                {

                    instance.Title = key.Parent.Path;
                    instance.Add(key, context.Entries);
                    
                    visualizationGrid.Children.Add(instance);

                }
            }
        }
        
        private void ChartWindow_Closed(object sender, EventArgs e)
        {
            VisualizationWindow w = (VisualizationWindow)sender;

            w.Dispose();
            Windows.Remove(w);
        }

        private void ContextMenu_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            try {
                e.CanExecute = treeView.SelectedValue != null;
            }
            catch(NullReferenceException) {
                e.CanExecute = false;
            }
        }

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

    }
}
