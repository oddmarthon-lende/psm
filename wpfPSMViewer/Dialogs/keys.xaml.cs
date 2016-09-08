using PSMonitor;
using PSMViewer.Models;
using PSMViewer.ViewModels;
using PSMViewer.Visualizations;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Xceed.Wpf.Toolkit;

namespace PSMViewer.Dialogs
{
    /// <summary>
    /// Interaction logic for modify_data_title.xaml
    /// </summary>
    public partial class KeyEditor : Window
    {
        
        public class Item
        {
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

            public KeyItem Key { get; private set; }

            public ObservableCollection<Component> Components { get; private set; } = new ObservableCollection<Component>();

            public Item(KeyItem key)
            {

                Key = key;

                string[] path = key.Path.Split('.');

                foreach(string p in path)
                {

                    Components.Add(new Component(p, key.Title, this));

                }

        }

        }

        public ObservableCollection<Item> Items { get; private set; } = new ObservableCollection<Item>();
        
        private VisualizationControl _widget;



        public Visibility TreeVisibility
        {
            get { return (Visibility)GetValue(TreeVisibilityProperty); }
            set { SetValue(TreeVisibilityProperty, value); }
        }
        public static readonly DependencyProperty TreeVisibilityProperty =
            DependencyProperty.Register("TreeVisibility", typeof(Visibility), typeof(KeyEditor), new FrameworkPropertyMetadata(Visibility.Visible, (sender, args) => {

                KeyEditor editor = (KeyEditor)sender;
                editor.keys.SetValue(Grid.ColumnProperty, (Visibility)args.NewValue != Visibility.Visible ? 0 : 1);
                editor.keys.SetValue(Grid.ColumnSpanProperty, (Visibility)args.NewValue != Visibility.Visible ? 2 : 1);

            }));



        public bool CanDelete
        {
            get { return (bool)GetValue(CanDeleteProperty); }
            set { SetValue(CanDeleteProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CanDelete.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CanDeleteProperty =
            DependencyProperty.Register("CanDelete", typeof(bool), typeof(KeyEditor), new PropertyMetadata(true));



        public bool CanAdd
        {
            get { return (bool)GetValue(CanAddProperty); }
            set { SetValue(CanAddProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CanAdd.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CanAddProperty =
            DependencyProperty.Register("CanAdd", typeof(bool), typeof(KeyEditor), new PropertyMetadata(true));





        public KeyEditor(VisualizationControl widget) : this()
        {

            _widget = widget;

            foreach (MultiControl control in _widget.Controls)
                Items.Add(new Item(control.Key));


        }

        public KeyEditor(params Item[] items) : this()
        {

            foreach (Item item in items)
                Items.Add(item);

            
        }

        public KeyEditor()
        {
            InitializeComponent();
            this.OnReload(treeView);
            treeView.MouseDoubleClick += TreeView_MouseDoubleClick;
        }

        private void TreeView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Add_Button_Click(null, null);
        }

        private void TextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {

            Item.Component component = (Item.Component)((TextBlock)e.OriginalSource).DataContext;
            component.Select();
        }

        private void Add_Button_Click(object sender, RoutedEventArgs e)
        {

            foreach (KeyItem key in KeyItem.Parse(path_field.Text))
            {

                if (this._widget != null && key != null && key.Type != null)
                {
                    if (this._widget.Add(key))
                        Items.Add(new Item(this._widget.GetControl(key).Key));
                }

            }

        }

        private void Remove_Button_Click(object sender, RoutedEventArgs e)
        {

            Item item = ((Item)((Button)sender).DataContext);
            
            if (this._widget != null)
            {

                this._widget.Remove(item.Key);
                Items.Remove(item);

            }
                

        }

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
