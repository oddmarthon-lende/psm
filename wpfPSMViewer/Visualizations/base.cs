using PSMViewer.Models;
using PSMViewer.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Windows.Controls;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using Xceed.Wpf.Toolkit.PropertyGrid;
using System.Windows.Input;
using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;

namespace PSMViewer.Visualizations
{
    public class KeyItemPathList : List<string> {}

    public class VisualizationControl : ContentControl, IDisposable, IReload, INotifyPropertyChanged
    {
        
        #region Static Properties and Methods

        public class InheritorInfo : INotifyPropertyChanged
        {

            public event PropertyChangedEventHandler PropertyChanged;

            protected virtual void OnPropertyChanged(string propertyName)
            {
                PropertyChangedEventHandler handler = PropertyChanged;
                if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
            }
            protected bool SetField<TField>(ref TField field, TField value, [CallerMemberName] string propertyName = "")
            {
                if (EqualityComparer<TField>.Default.Equals(field, value)) return false;
                field = value;
                OnPropertyChanged(propertyName);

                return true;
            }

            public string DisplayName { get; private set; }
            public string Icon { get; private set; }
            public Type Type { get; private set; }

            private bool _isSelected = false;
            public bool IsSelected { get { return _isSelected; } set { SetField(ref _isSelected, value); } }

            public InheritorInfo(string DisplayName, string Icon, Type Type)
            {
                this.DisplayName = DisplayName;
                this.Icon = Icon;
                this.Type = Type;
            }
        }

        public static List<InheritorInfo> List {

            get {

                return Assembly.GetExecutingAssembly().DefinedTypes.Where(info =>
                {

                    if (info.BaseType == null || !info.IsSubclassOf(typeof(VisualizationControl)))
                        return false;

                    if (info.ContainsGenericParameters)
                    {
                        return false;
                    }

                    PropertyInfo isVisible = info.GetProperty("isVisible", BindingFlags.Public | BindingFlags.Static);

                    return isVisible == null || (bool)isVisible.GetValue(null);

                })
                .Select(info =>
                {

                    PropertyInfo DisplayName = info.GetProperty("DisplayName", BindingFlags.Public | BindingFlags.Static);
                    PropertyInfo Icon = info.GetProperty("Icon", BindingFlags.Public | BindingFlags.Static);

                    return new InheritorInfo(
                        DisplayName == null ? "" : (string)DisplayName.GetValue(null),
                        Icon == null ? "" : (string)Icon.GetValue(null),
                        info
                    );

                }).ToList();
            }
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

        #region Properties

        #region Dependency Properties

        public Visibility NavigationVisibility
        {
            get { return (Visibility)GetValue(NavigationVisibilityProperty); }
            set {
                SetValue(NavigationVisibilityProperty, value);
            }
        }
        public static readonly DependencyProperty NavigationVisibilityProperty =
            DependencyProperty.Register("NavigationVisibility", typeof(Visibility), typeof(VisualizationControl), new FrameworkPropertyMetadata(Visibility.Collapsed, FrameworkPropertyMetadataOptions.AffectsRender, (sender, e) =>
            {
                VisualizationControl w = (VisualizationControl)sender;

                if (((Visibility)e.NewValue) != Visibility.Visible && ((Visibility)e.OldValue) == Visibility.Visible)
                    w.PopState();
                else if (((Visibility)e.NewValue) == Visibility.Visible && ((Visibility)e.OldValue) != Visibility.Visible)
                    w.PushState();

            }));    
            
                
        
        public string Title
        {
            get {
                return (string)GetValue(TitleProperty);
            }
            set {
                SetValue(TitleProperty, value);
            }
        }
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(VisualizationControl), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));




        public string SubTitle
        {
            get { return (string)GetValue(SubTitleProperty); }
            set { SetValue(SubTitleProperty, value); }
        }
        public static readonly DependencyProperty SubTitleProperty =
            DependencyProperty.Register("SubTitle", typeof(string), typeof(VisualizationControl), new PropertyMetadata(null));



        

        public double TitleFontSize
        {
            get { return (double)GetValue(TitleFontSizeProperty); }
            set { SetValue(TitleFontSizeProperty, value); }
        }
        public static readonly DependencyProperty TitleFontSizeProperty =
            DependencyProperty.Register("TitleFontSize", typeof(double), typeof(VisualizationControl), new PropertyMetadata(14D));





        public double SubTitleFontSize
        {
            get { return (double)GetValue(SubTitleFontSizeProperty); }
            set { SetValue(SubTitleFontSizeProperty, value); }
        }
        public static readonly DependencyProperty SubTitleFontSizeProperty =
            DependencyProperty.Register("SubTitleFontSize", typeof(double), typeof(VisualizationControl), new PropertyMetadata(12D));




        public FontWeight TitleFontWeight
        {
            get { return (FontWeight)GetValue(TitleFontWeightProperty); }
            set { SetValue(TitleFontWeightProperty, value); }
        }public static readonly DependencyProperty TitleFontWeightProperty =
            DependencyProperty.Register("TitleFontWeight", typeof(FontWeight), typeof(VisualizationControl), new PropertyMetadata(FontWeights.Bold));




        public Visibility TitleVisibility
        {
            get { return (Visibility)GetValue(TitleVisibilityProperty); }
            set { SetValue(TitleVisibilityProperty, value); }
        }
        public static readonly DependencyProperty TitleVisibilityProperty =
            DependencyProperty.Register("TitleVisibility", typeof(Visibility), typeof(VisualizationControl), new PropertyMetadata(Visibility.Hidden));




        public Window Owner
        {
            get { return (Window)GetValue(OwnerProperty); }
            set { SetValue(OwnerProperty, value); }
        }
        public static readonly DependencyProperty OwnerProperty =
            DependencyProperty.Register("Owner", typeof(Window), typeof(VisualizationControl), new PropertyMetadata(null));

        #endregion

        public Guid Id { get; set; } = Guid.NewGuid();

        public int Index
        {
            get
            {
                return ((VisualizationWindow)this.Owner).Children.IndexOf(this);
            }
        }
        
        protected List<PropertyDefinition> PropertyDefinitions { get; set; } = new List<PropertyDefinition>();

        private KeyItemPathList keys = new KeyItemPathList();
        public KeyItemPathList Keys
        {
            get
            {
                return keys;
            }

            set
            {
                foreach (string path in value)
                {
                    Add(KeyItem.CreateFromPath(path));
                }
            }
        }

        private ObservableCollection<MultiControl> controls = new ObservableCollection<MultiControl>();
        public ObservableCollection<MultiControl> Controls
        {
            get
            {
                return controls;
            }
        }

        public TimeSpan Timespan { get; set; } = new TimeSpan(1, 0, 0, 0, 0);

        public long Count { get; set; }      = 1L;
        public long StartIndex { get; set; } = 0L;

        public ControlType SelectedControlType { get; set; } = ControlType.Index;

        public int? Row {

            get {
                return (int?)(GetValue(Grid.RowProperty)??0);
            }

            set {
                SetValue(Grid.RowProperty, Math.Max(0, value ?? 0));
            }
        }
        public int? RowSpan
        {

            get
            {
                return (int?)(GetValue(Grid.RowSpanProperty) ?? 1);
            }

            set
            {
                SetValue(Grid.RowSpanProperty, Math.Max(1, value??1));
            }
        }
        public int? Column
        {

            get
            {
                return (int?)(GetValue(Grid.ColumnProperty) ?? 0);
            }

            set
            {
                SetValue(Grid.ColumnProperty, Math.Max(0, value ?? 0));
            }
        }
        public int? ColumnSpan
        {

            get
            {
                return (int?)(GetValue(Grid.ColumnSpanProperty) ?? 1);
            }

            set
            {
                SetValue(Grid.ColumnSpanProperty, Math.Max(1, value ?? 1));
            }
        }
        
        #endregion

        public enum ControlType
        {
            Time,
            Index
        }

        #region Commands

        protected enum CommandType
        {
            ADD_KEY,
            REMOVE_KEY,
            PROPERTIES,
            PREV,
            NEXT
        }

        public class MultiControl : IDisposable
        {
            public KeyItem Key { get; private set; }
            public ObservableCollection<EntryItem> Entries { get; set; }
            public event DataChangedEventHandler DataChanged;

            private Dictionary<ControlType, Controls> Controls = new Dictionary<ControlType, ViewModels.Controls>();

            public MultiControl(KeyItem key, ObservableCollection<EntryItem> Entries = null)
            {

                this.Key = key;
                this.Entries = Entries ?? new ObservableCollection<EntryItem>();

                Controls.Add(ControlType.Index, new Controls<long, long>(this.Entries, 0, 1) { Selected = key });
                Controls.Add(ControlType.Time, new Controls<DateTime, TimeSpan>(this.Entries, null, new TimeSpan()) { Selected = key });

                Stack = new Stack<Dictionary<ControlType, ViewModels.Controls>>();
                                
                foreach (var pair in Controls)
                {
                    pair.Value.DataChanged += Value_DataChanged;
                }

            }

            private Stack<Dictionary<ControlType, ViewModels.Controls>> Stack;

            public void PushState()
            {

                Dictionary<ControlType, Controls> c = new Dictionary<ControlType, ViewModels.Controls>();

                Stack.Push(c);

                foreach(KeyValuePair<ControlType, Controls> pair in Controls)
                {
                    c[pair.Key] = (Controls)Activator.CreateInstance(pair.Value.GetType(), Controls[pair.Key]);
                    c[pair.Key].DataChanged += Value_DataChanged;
                }
            }

            public void PopState()
            {

                Dictionary<ControlType, Controls> c = Stack.Pop();
                
                if (c != null)
                {
                    foreach (KeyValuePair<ControlType, Controls> pair in c)
                    {
                        Controls[pair.Key].Dispose();
                        Controls[pair.Key] = c[pair.Key];
                    }
                    
                }
            }

            private void Value_DataChanged(object sender)
            {
                if (DataChanged != null)
                    DataChanged(sender);  
            }

            public Controls Get(ControlType type, object Start = null, object Count = null)
            {
                
                Controls c = Controls[type];

                if(c != null)
                {

                    if(Start != null)
                        c.Start = Start;

                    if(Count != null)
                        c.Count = Count;
                }

                return c;
            }

            public void Dispose()
            {

                foreach (var pair in Controls)
                {
                    pair.Value.DataChanged -= Value_DataChanged;
                }

                foreach (var pair in Controls)
                {
                    pair.Value.Dispose();
                }
            }
        }

        public ReadOnlyDictionary<string, RelayCommand> Commands
        {
            get
            {
                return new ReadOnlyDictionary<string, RelayCommand>(CommandsSource);
            }
        }

        protected CommandCollection CommandsSource = new CommandCollection();
        
        protected virtual void ExecuteCommand(object sender, object parameter)
        {

            CommandType cmd = (CommandType)((RelayCommand)sender).Arguments[0].Value;

            Tree tree = null;
            Window window = null;
            
            switch (cmd)
            {

                case CommandType.ADD_KEY:
                case CommandType.REMOVE_KEY:

                    tree = new Tree();
                    window = tree.Window;

                    window.Owner = this.Owner;
                    window.Width = this.Owner.Width * .5;
                    window.Height = this.Owner.Height * 1.25;

                    break;

            }

            switch (cmd)
            {
                
                case CommandType.NEXT:

                    Previous();
                    return;

                case CommandType.PREV:
                                        
                    Next();
                    return;

                case CommandType.ADD_KEY:

                    window.Title = String.Format("Add Key [{0}]", Title);

                    ((MainWindow)App.Current.MainWindow).Reload(tree);

                    tree.Window.ShowDialog();

                    if(tree.SelectedValue != null)
                        Add((KeyItem)tree.SelectedValue);
                                        
                    break;

                case CommandType.REMOVE_KEY:

                    window.Title = String.Format("Remove Key [{0}]", Title);

                    ((TreeViewItem)tree.Items[0]).ItemsSource = Keys.Select(path => { return KeyItem.CreateFromPath(path); });

                    tree.Window.ShowDialog();

                    if (tree.SelectedValue != null)
                        Remove((KeyItem)tree.SelectedValue);

                    break;

                case CommandType.PROPERTIES:

                    (new PropertiesWindow(this, PropertyDefinitions.ToArray()) {
                        Title = String.Format("Properties [{0}]", this.Title),
                        ShowInTaskbar = false,
                        Owner = this.Owner,
                        Width = this.Owner.ActualWidth * .75
                    }).ShowDialog();
                    
                    break;
            }

            ((MainWindow)App.Current.MainWindow).Reload(this);

        }

        #endregion

        protected Func<object, object, bool> canExecute = delegate { return true; };

        public VisualizationControl()
        {
            
            DataContext = this;
            Template    = (ControlTemplate)FindResource("VisualizationControlTemplate");
            Title       = String.Format("<{0}> [{1}]", GetType().Name, Id);
            Margin      = new Thickness(5);

            #region Commands

            CommandsSource.Add("Add", new RelayCommand(ExecuteCommand, canExecute, CommandType.ADD_KEY));
            CommandsSource.Add("Remove", new RelayCommand(ExecuteCommand, canExecute, CommandType.REMOVE_KEY));
            CommandsSource.Add("Properties", new RelayCommand(ExecuteCommand, canExecute, CommandType.PROPERTIES));
            CommandsSource.Add("Previous", new RelayCommand(ExecuteCommand, canExecute, CommandType.PREV));
            CommandsSource.Add("Next", new RelayCommand(ExecuteCommand, canExecute, CommandType.NEXT));

            #endregion

            #region PropertyDefinitions
            
                PropertyDefinitions.Add(new PropertyDefinition()
                {
                    Category = "Layout",
                    TargetProperties = new List<object>(new string[] { "Row", "RowSpan", "Column", "ColumnSpan", "Margin" })
                });

                PropertyDefinitions.Add(new PropertyDefinition()
                { 
                    Category = "Controls",
                    TargetProperties = new List<object>(new string[] { "Timespan", "Count", "StartIndex", "SelectedControlType" })
                });

                PropertyDefinitions.Add(new PropertyDefinition()
                {
                    Category = "Common",
                    TargetProperties = new List<object>(new string[] { "FontStyle", "FontFamily", "FontWeight", "FontSize", "BorderThickness" })
                });

                PropertyDefinitions.Add(new PropertyDefinition()
                {
                    Category = "Title",
                    TargetProperties = new List<object>(new string[] { "Title", "SubTitle", "TitleVisibility", "TitleFontSize", "SubTitleFontSize", "TitleFontWeight"  })
                }); 

                PropertyDefinitions.Add(new PropertyDefinition()
                {
                    IsExpandable = true,
                    Category = "Colors",
                    TargetProperties = new List<object>(new string[] { "Background", "Foreground", "BorderBrush" })
                });

            

            #endregion

            #region UserCommands

            RegisterUserCommand("Properties", CommandsSource["Properties"]);
            RegisterUserCommand();
            RegisterUserCommand("Remove Key(s)", CommandsSource["Remove"]);
            RegisterUserCommand("Add Key(s)", CommandsSource["Add"]);
                        
            #endregion

            ContextMenu = new ContextMenu();
            ContextMenu.ContextMenuOpening += delegate
            {
                ContextMenu.ItemsSource = MenuItems;
                NavigationVisibility = Visibility.Collapsed;
            };

            this.SizeChanged += delegate {
                Refresh();
            };           
            
        }

        protected Dictionary<string, object> _userCommands = new Dictionary<string, object>();
        public virtual IEnumerable<Control> MenuItems {

            get
            {
                return _userCommands.Reverse().Select<KeyValuePair<string, object>, Control>(pair =>
                {

                    if (pair.Value != null)
                    {

                        if (pair.Value is RelayCommand)
                            return new MenuItem() { Header = pair.Key, Command = (ICommand)pair.Value, DataContext = null };
                        else
                            return (Control)pair.Value;
                    }

                    else
                        return new Separator();

                });
            }
        }

        public void RegisterUserCommand(string title = null, ICommand command = null)
        {
            if (title != null && command != null)
                _userCommands.Add(title, command);
            else
                _userCommands.Add(Guid.NewGuid().ToString(), null);

            OnPropertyChanged("MenuItems");
        }

        public virtual void Refresh()
        {

            DependencyObject parent = this.Parent;

            if(parent != null)
            {
                FrameworkElement p = (FrameworkElement)parent;

                p.InvalidateArrange();
                p.InvalidateMeasure();
                p.InvalidateVisual();
            }
            else
            {
                InvalidateArrange();
                InvalidateMeasure();
                InvalidateVisual();
            }
            
        }

        public virtual void Dispose()
        {
            foreach(MultiControl m in controls.ToArray())
            {
                Remove(m.Key);
                m.Dispose();
            }

            controls.Clear();

            GC.SuppressFinalize(this);
        }

        protected MultiControl GetControlsFor(KeyItem key)
        {
            return (from s in controls where s.Key.Path == key.Path select s).ElementAtOrDefault(0);
        }

        public override bool ShouldSerializeContent()
        {
            return false;
        }

        protected override bool ShouldSerializeProperty(DependencyProperty dp)
        {
            DependencyProperty[] properties = new DependencyProperty[]
            {
                Grid.RowProperty,
                Grid.RowSpanProperty,
                Grid.ColumnProperty,
                Grid.ColumnSpanProperty,
                OwnerProperty,
                ContextMenuProperty,
                NameProperty,
                NavigationVisibilityProperty,
                TemplateProperty,
            };

            foreach (DependencyProperty p in properties)
            {
                if (dp == p) return false;
            }
            
            return base.ShouldSerializeProperty(dp);
        }

        public virtual MultiControl Add(KeyItem key, ObservableCollection<EntryItem> collection = null)
        {

            if (key.Type == null)
            {
                Keys.Add(key.Path);

                key.Children.CollectionChanged += Children_CollectionChanged;

                ((MainWindow)App.Current.MainWindow).Reload(key);

                return null;
            }

            MultiControl control = GetControlsFor(key);

            if (control == null)
            {
                control = new MultiControl(key, collection);

                controls.Add(control);

                if (key.Parent != null && !keys.Contains(key.Parent.Path))
                    keys.Add(key.Path);

            }

            Refresh();

            return control;

        }

        public virtual void Remove(KeyItem key)
        {

            key.Children.CollectionChanged -= Children_CollectionChanged;

            foreach(MultiControl m in (from s in Controls.ToArray() where s.Key.Path == key.Path || (s.Key.Parent != null && s.Key.Parent.Path == key.Path) select s))
            {
                Controls.Remove(m);
                m.Dispose();                
            }

            Keys.Remove(key.Path);
            Refresh();

        }

        protected void Children_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {

            foreach (MultiControl m in (from s in controls select s))
            {
                if (e.OldItems != null && (from KeyItem k in e.OldItems where k.Path == m.Key.Path select k).ElementAtOrDefault(0) == null)
                    Remove(m.Key);
            }

            if (e.NewItems == null) return;

            foreach (KeyItem k in e.NewItems)
            {
                
                if (k.Type != null && (from s in controls where s.Key.Path == k.Path select s).ElementAtOrDefault(0) == null)
                    Add(k);

            }

            Refresh();

        }

        public virtual void Reload()
        {
            
            object Start = null;
            object Count = null;

            switch(SelectedControlType)
            {
                
                case ControlType.Index:

                    Start = StartIndex;
                    Count = this.Count;

                    break;

                case ControlType.Time:

                    Count = Timespan;

                    break;
            }

            foreach (MultiControl m in controls)
            {

                Controls control = m.Get(SelectedControlType, Start, Count);
                KeyItem key     = m.Key;
                
                key.Reload();
                control.Reload();
            }

            Refresh();

        }
                
        public virtual bool Next()
        {
            bool yn = false;

            foreach (MultiControl control in controls)
            {
                yn |= control.Get(SelectedControlType).Next();
            }

            return yn;
        }

        public virtual bool Previous()
        {

            bool yn = false;

            foreach (MultiControl control in controls)
            {
                yn |= control.Get(SelectedControlType).Previous();
            }

            return yn;
        }

        public virtual void PushState()
        {
            foreach (MultiControl control in controls)
            {
                control.PushState();
            }
        }

        public virtual void PopState()
        {
            foreach (MultiControl control in controls)
            {
                control.PopState();
            }
        }
    }
}
