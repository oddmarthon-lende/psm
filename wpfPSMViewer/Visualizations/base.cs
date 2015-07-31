using PSMViewer.Models;
using PSMViewer.Properties;
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

namespace PSMViewer.Visualizations
{
    public class KeyItemPathList : List<string> {}
    
    public class VisualizationControl : UserControl, IDisposable, IReload, INotifyPropertyChanged
    {
        #region Static Properties and Methods
        
        public static Type SetChartType(KeyItem key, Type chartType)
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

        public static Type GetChartType(KeyItem key)
        {

            Settings.Default.chartType = Settings.Default.chartType ?? new StringCollection();

            Type chartType = (from string t in Settings.Default.chartType
                              where (t.StartsWith(key.Path))
                              select Type.GetType(t.Split(',')[1])).ElementAtOrDefault(0);

            return chartType;

        }
        
        public static VisualizationControl Restore(KeyItem key)
        {
            Type t = VisualizationControl.GetChartType(key);
            return t == null ? null : (VisualizationControl)Activator.CreateInstance(t);
        }

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

        public Guid Id { get; set; } = Guid.NewGuid();

        private string _title = null;
        public virtual string Title {

            get {
                return (String.IsNullOrEmpty(_title) || String.IsNullOrWhiteSpace(_title) ? null : _title) ?? String.Format("<{0}> [{1}]", GetType().Name, Index );
            }

            set {
                SetField(ref _title, value);
            }
        }

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

        public long Count { get; set; } = 1;
        public long StartIndex { get; set; } = 0;

        public ControlType SelectedControlType { get; set; } = ControlType.Manual;

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

        public Window Owner
        {
            get { return (Window)GetValue(OwnerProperty); }
            set { SetValue(OwnerProperty, value); }
        }
        public static readonly DependencyProperty OwnerProperty =
            DependencyProperty.Register("Owner", typeof(Window), typeof(VisualizationControl), new PropertyMetadata(null));
               

        #endregion

        public enum ControlType
        {
            Time,
            Index,
            Manual
        }

        #region Commands

        protected enum CommandType
        {
            ADD,
            REMOVE,
            PROPERTIES
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

                foreach(var pair in Controls)
                {
                    pair.Value.DataChanged += Value_DataChanged;
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

            RelayCommand cmd = (RelayCommand)sender;
            Tree tree = null;

            switch ((CommandType)cmd.Arguments[0].Value)
            {
                case CommandType.ADD:

                    tree = new Tree();

                    tree.Window.ShowDialog();

                    if(tree.SelectedValue != null)
                        Add((KeyItem)tree.SelectedValue);
                                        
                    break;

                case CommandType.REMOVE:

                    tree = new Tree();
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
            
            this.DataContext = this;
            this.Template = (ControlTemplate)FindResource("VisualizationControlTemplate");            
            
            #region Commands
           
            CommandsSource.Add("Add", new RelayCommand(ExecuteCommand, canExecute, CommandType.ADD));
            CommandsSource.Add("Remove", new RelayCommand(ExecuteCommand, canExecute, CommandType.REMOVE));
            CommandsSource.Add("Properties", new RelayCommand(ExecuteCommand, canExecute, CommandType.PROPERTIES));

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
                    TargetProperties = new List<object>(new string[] { "Title", "FontStyle", "FontFamily", "FontWeight", "FontSize", "BorderThickness" })
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
            ContextMenuOpening += delegate
            {
                ContextMenu.ItemsSource = UserCommands;
            };

            this.SizeChanged += delegate {
                Refresh();
            };
            
        }

        protected Dictionary<string, ICommand> _userCommands = new Dictionary<string, ICommand>();
        public virtual IEnumerable<Control> UserCommands {

            get
            {
                return _userCommands.Reverse().Select<KeyValuePair<string, ICommand>, Control>(pair => {

                    if (pair.Value != null)
                        return new MenuItem() { Header = pair.Key, Command = pair.Value, DataContext = null };
                    else
                        return new Separator();

                });
            }
        }

        public virtual void RegisterUserCommand(string title = null, ICommand command = null)
        {
            if (title != null && command != null)
                _userCommands.Add(title, command);
            else
                _userCommands.Add(Guid.NewGuid().ToString(), null);
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

        public MultiControl GetControlsFor(KeyItem key)
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
                NameProperty
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
                case ControlType.Manual:

                    return;                

                case ControlType.Index:

                    Start = StartIndex;
                    Count = this.Count;

                    break;

                case ControlType.Time:

                    Count = Timespan;

                    break;
            }

            foreach (MultiControl m in (from p in controls select p))
            {

                Controls control = m.Get(SelectedControlType, Start, Count);
                KeyItem key      = m.Key;
                
                key.Reload();
                control.Reload();
            }

            Refresh();

        }
                
        public virtual bool Next()
        {
            throw new NotImplementedException();
        }

        public virtual bool Previous()
        {
            throw new NotImplementedException();
        }
    }
}
