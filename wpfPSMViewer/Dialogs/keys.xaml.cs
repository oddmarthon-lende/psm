using PSMonitor;
using PSMViewer.Models;
using PSMViewer.ViewModels;
using PSMViewer.Visualizations;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Xceed.Wpf.Toolkit;

namespace PSMViewer.Dialogs
{
    /// <summary>
    /// Interaction logic for modify_data_title.xaml
    /// </summary>
    public partial class KeyEditor : Window
    {
        /// <summary>
        /// 
        /// </summary>
        public class Item
        {

            /// <summary>
            /// 
            /// </summary>
            public class Component : INotifyPropertyChanged
            {

                public string Name { get; private set; }

                public KeyItemTitle Title { get; private set; }

                public Item Item { get; private set; }

                public event PropertyChangedEventHandler PropertyChanged;

                protected virtual void OnPropertyChanged(string propertyName)
                {
                    PropertyChangedEventHandler handler = PropertyChanged;
                    if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
                }

                public SolidColorBrush Foreground {

                    get
                    {
                        return Title.Position == Index ? Brushes.White : Brushes.Black; 
                    }

                }

                public SolidColorBrush Background
                {

                    get
                    {
                        return Title.Position == Index ? Brushes.Black : Brushes.White;
                    }

                }

                public uint Index
                {

                    get
                    {
                        return (uint)Item.Components.IndexOf(this);
                    }
            }

                public Component(string name, KeyItemTitle title, Item item)
                {

                    Name = name;
                    Title = title;
                    Item = item;
                }

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
            public IKeyItem Key { get; private set; }

            /// <summary>
            /// 
            /// </summary>
            public ObservableCollection<Component> Components { get; private set; } = new ObservableCollection<Component>();

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="key"></param>
            public Item(IKeyItem key)
            {

                Key = key;

                string[] path = key.Path.Split('.');

                foreach(string p in path)
                {

                    Components.Add(new Component(p, key.Title, this));

                }

        }

        }

        /// <summary>
        /// 
        /// </summary>
        public ObservableCollection<Item> Items { get; private set; } = new ObservableCollection<Item>();
        
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

            foreach (MultiControl control in _widget.Controls)
                Items.Add(new Item(control.Key));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="items"></param>
        public KeyEditor(params Item[] items) : this()
        {

            foreach (Item item in items)
                Items.Add(item);            
        }

        /// <summary>
        /// 
        /// </summary>
        public KeyEditor()
        {
            InitializeComponent();
            this.OnReload(treeView);
            treeView.MouseDoubleClick += TreeView_MouseDoubleClick;
            Icon = BitmapFrame.Create(Assembly.GetExecutingAssembly().GetManifestResourceStream("PSMViewer.Icons.application_form_edit.png"));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TreeView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Add_Button_Click(null, null);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {

            Item.Component component = (Item.Component)((TextBlock)e.OriginalSource).DataContext;
            component.Select();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Add_Button_Click(object sender, RoutedEventArgs e)
        {

            if (this._widget != null && treeView.Key != null && treeView.Key.Type != null)
            {
                KeyItemW itemw = KeyItemW.Create(path_field.Text);
                int i = 0;
                bool [] success = this._widget.Add(itemw);

                foreach (KeyItem key in itemw.Children)
                {
                    if (success[i++] && key != null && key.Type != null)
                    {
                        this._widget.Defaults.Keys.CopyTo(key);
                        Items.Add(new Item(this._widget.GetControl(key).Key));
                    }

                }
            }           

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Remove_Button_Click(object sender, RoutedEventArgs e)
        {

            Item item = ((Item)((Button)sender).DataContext);
            
            if (this._widget != null)
            {

                if(item.Key.W == null && this._widget.Remove(item.Key))
                    Items.Remove(item);
                else if(item.Key.W != null)
                {
                    if (System.Windows.MessageBox.Show("This item was added using wildcards, do you want to remove all items added by the wildcard?", "Cannot remove item", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        this._widget.Remove(item.Key.W);

                        foreach(Item it in Items.ToArray())
                        {
                            if (item.Key.W.Children.Contains(it.Key))
                                Items.Remove(it);

                        }
                    }
                }

            }
                

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CopyToAll_Button_Click(object sender, RoutedEventArgs e)
        {
            Item item = ((Item)((Button)sender).DataContext);

            foreach(Item t in Items)
            {
                if(t != item)
                    item.Key.CopyTo(t.Key);
            }

            ((DependencyObject)this.Content).UpdateBindingTargets(ComboBox.SelectedValueProperty, Xceed.Wpf.Toolkit.DoubleUpDown.ValueProperty);

        }
    }
}
