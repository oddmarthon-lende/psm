using PSM.Viewer.Models;
using PSM.Viewer.Visualizations;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Threading;

namespace PSM.Viewer.Dialogs.Commands
{

    public class KeyEditorCopyCommand : ICommand
    {

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public KeyEditor Editor { get; set; }

        public void Execute(object parameter)
        {

            DataGridCell cell = (DataGridCell)parameter;
            DataGridColumn column = cell.Column;
            KeyEditor.KeyEditorItem item = (KeyEditor.KeyEditorItem)cell.DataContext;
            
            foreach(KeyEditor.KeyEditorItem it in Editor.Items)
            {

                if (item == it)
                    continue;

                switch (column.DisplayIndex)
                {

                    case 0:

                        it.Key.Title.Position = item.Key.Title.Position;
                        break;

                    case 1:

                        it.Key.Title.Mode = item.Key.Title.Mode;
                        break;

                    case 2:

                        it.Key.Title.Alias = item.Key.Title.Alias;
                        break;

                    case 3:

                        it.Key.Conversion.Mode = item.Key.Conversion.Mode;
                        break;

                    case 4:

                        it.Key.Conversion.Value = item.Key.Conversion.Value;
                        break;

                    case 5:

                        it.Key.Color = item.Key.Color;
                        break;


                }

            }

            ((DependencyObject)Editor.Content).UpdateBindingTargets(ComboBox.SelectedValueProperty, TextBlock.TextProperty);
        }
    }

}

namespace PSM.Viewer.Dialogs
{

    /// <summary>
    /// Interaction logic for modify_data_title.xaml
    /// </summary>
    public partial class KeyEditor : Theme.Window, IReload
    {
        
        public Commands.KeyEditorCopyCommand CopyCommand { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public class KeyEditorItem
        {

            /// <summary>
            /// 
            /// </summary>
            public class Component : INotifyPropertyChanged
            {
                /// <summary>
                /// 
                /// </summary>
                public string Name { get; private set; }

                /// <summary>
                /// 
                /// </summary>
                public KeyItemTitle Title { get; private set; }

                /// <summary>
                /// 
                /// </summary>
                public KeyEditorItem Item { get; private set; }

                /// <summary>
                /// 
                /// </summary>
                public event PropertyChangedEventHandler PropertyChanged;

                /// <summary>
                /// 
                /// </summary>
                /// <param name="propertyName"></param>
                protected virtual void OnPropertyChanged(string propertyName)
                {
                    PropertyChangedEventHandler handler = PropertyChanged;
                    if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
                }

                /// <summary>
                /// 
                /// </summary>
                private SolidColorBrush _color0 = (SolidColorBrush)App.Current.FindResource("MainColorDark");

                /// <summary>
                /// 
                /// </summary>
                private SolidColorBrush _color1 = (SolidColorBrush)App.Current.FindResource("TextIconColor");

                /// <summary>
                /// 
                /// </summary>
                public SolidColorBrush Foreground {

                    get
                    {
                        return Title.Position == Index ? _color0 : _color1; 
                    }

                }

                /// <summary>
                /// 
                /// </summary>
                public SolidColorBrush Background
                {

                    get
                    {
                        return Title.Position == Index ? _color1 : _color0;
                    }

                }

                /// <summary>
                /// 
                /// </summary>
                public uint Index
                {

                    get
                    {
                        return (uint)Item.Components.IndexOf(this);
                    }
                }

                /// <summary>
                /// 
                /// </summary>
                /// <param name="name"></param>
                /// <param name="title"></param>
                /// <param name="item"></param>
                public Component(string name, KeyItemTitle title, KeyEditorItem item)
                {

                    Name = name;
                    Title = title;
                    Item = item;
                }

                /// <summary>
                /// 
                /// </summary>
                public void Select()
                {
                    Title.Position = Index;

                    foreach(Component c in Item.Components)
                    {
                        c.OnPropertyChanged("Foreground");
                        c.OnPropertyChanged("Background");
                    }
                }
            }

            /// <summary>
            /// 
            /// </summary>
            public string GroupName
            {
                get
                {
                    return Key.W != null ? Key.W.StaticPath : Key is KeyItemW ? "Groups" : "Keys";
                }
            }

            /// <summary>
            /// 
            /// </summary>
            public IKeyItem Key { get; private set; }

            /// <summary>
            /// 
            /// </summary>
            public ObservableCollection<Component> Components { get; private set; } = new ObservableCollection<Component>();

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="key"></param>
            public KeyEditorItem(IKeyItem key)
            {

                Key = key;

                string[] path = key.StaticPath.Split('.');

                foreach(string p in path)
                {
                    Components.Add(new Component(p, key.Title, this));
                }

            }

        }

        /// <summary>
        /// 
        /// </summary>
        public ObservableCollection<KeyEditorItem> Items { get; private set; } = new ObservableCollection<KeyEditorItem>();
        
        /// <summary>
        /// 
        /// </summary>
        private VisualizationControl _widget;

        /// <summary>
        /// 
        /// </summary>
        public Visibility TreeVisibility
        {
            get { return (Visibility)GetValue(TreeVisibilityProperty); }
            set { SetValue(TreeVisibilityProperty, value); }
        }
        /// <summary>
        /// 
        /// </summary>
        public static readonly DependencyProperty TreeVisibilityProperty =
            DependencyProperty.Register("TreeVisibility", typeof(Visibility), typeof(KeyEditor), new FrameworkPropertyMetadata(Visibility.Visible, (sender, args) => {

                KeyEditor editor = (KeyEditor)sender;
                editor.keys.SetValue(Grid.ColumnProperty, (Visibility)args.NewValue != Visibility.Visible ? 0 : 1);
                editor.keys.SetValue(Grid.ColumnSpanProperty, (Visibility)args.NewValue != Visibility.Visible ? 2 : 1);

            }));

        /// <summary>
        /// 
        /// </summary>
        public bool CanDelete
        {
            get { return (bool)GetValue(CanDeleteProperty); }
            set { SetValue(CanDeleteProperty, value); }
        }
        /// <summary>
        /// 
        /// </summary>
        public static readonly DependencyProperty CanDeleteProperty =
            DependencyProperty.Register("CanDelete", typeof(bool), typeof(KeyEditor), new PropertyMetadata(true));

        /// <summary>
        /// 
        /// </summary>
        public bool CanAdd
        {
            get { return (bool)GetValue(CanAddProperty); }
            set { SetValue(CanAddProperty, value); }
        }

        public CancellationTokenSource CancellationTokenSource
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public ReloadStatus Status
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public static readonly DependencyProperty CanAddProperty =
            DependencyProperty.Register("CanAdd", typeof(bool), typeof(KeyEditor), new PropertyMetadata(true));


        /// <summary>
        /// 
        /// </summary>
        /// <param name="widget"></param>
        public KeyEditor(VisualizationControl widget) : this()
        {
            _widget = widget;
            Reload();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="items"></param>
        public KeyEditor(params KeyEditorItem[] items) : this()
        {
            foreach (KeyEditorItem item in items)
                Items.Add(item);
        }

        /// <summary>
        /// 
        /// </summary>
        public KeyEditor()
        {

            InitializeComponent();

            this.OnReload(treeView);

            Icon = BitmapFrame.Create(Assembly.GetExecutingAssembly().GetManifestResourceStream("PSM.Viewer.Icons.application_form_edit.png"));

            CopyCommand = new Commands.KeyEditorCopyCommand() { Editor = this };            

        }

        private void keys_PreviewKeyDown(object sender, KeyEventArgs e)
        {

            DataGrid dg = sender as DataGrid;
            if (dg != null)
            {
                DataGridRow dgr = (DataGridRow)(dg.ItemContainerGenerator.ContainerFromIndex(dg.SelectedIndex));
                KeyEditorItem item = (KeyEditorItem)dgr.DataContext;

                if (e.Key == Key.Delete && !dgr.IsEditing)
                {
                    
                    if (this._widget != null)
                    {

                        if (item.Key.W == null && this._widget.Remove(item.Key)) { }
                        else if (item.Key.W != null)
                        {
                            if (System.Windows.MessageBox.Show("This item was added using a path expression, do you want to remove all items added by the expression?", "Cannot remove item", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                            {
                                this._widget.Remove(item.Key.W);                                
                            }
                        }

                    }

                    e.Handled = true;
                    Reload();
                }
            }

        }
                
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {

            KeyEditorItem.Component component = (KeyEditorItem.Component)((TextBlock)e.OriginalSource).DataContext;
            component.Select();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Add(object sender = null, RoutedEventArgs e = null)
        {

            if (this._widget != null && path_field.Text.Length > 0)
            {
                try
                {
                    this._widget.Add(KeyItemW.Create(path_field.Text));
                    Reload();                    
                }
                catch(Exception error)
                {
                    System.Windows.MessageBox.Show(error.Message, "Something went wrong", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                
            }           

        }

        public void Reload()
        {

            if (this._widget == null)
                return;

            Items.Clear();

            foreach (KeyItemW group in _widget.Groups)
                Items.Add(new KeyEditorItem(group));

            foreach (MultiControl control in _widget.Controls)
                Items.Add(new KeyEditorItem(control.Key));
        }

        public bool Next()
        {
            return false;
        }

        public bool Previous()
        {
            return false;
        }
    }
}
