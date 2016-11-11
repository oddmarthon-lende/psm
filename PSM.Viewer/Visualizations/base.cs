/// <copyright file="base.cs" company="Baker Hughes Incorporated">
/// Copyright (c) 2016 All Rights Reserved
/// </copyright>
/// <author>Odd Marthon Lende</author>
/// <summary>The base for all visualization controls</summary>
/// 
using PSM.Stores;
using PSM.Viewer.Editors;
using PSM.Viewer.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Xceed.Wpf.Toolkit.PropertyGrid;
using System.Collections;
using PSM.Viewer.Commands;
using PSM.Viewer.Dialogs;

namespace PSM.Viewer.Visualizations
{

    /// <summary>
    /// Used to specify the name that will displayed in the UI.
    /// </summary>
    class DisplayNameAttribute : Attribute
    {
        private string _name;

        public DisplayNameAttribute(string name)
        {
            _name = name;
        }

        public static explicit operator string(DisplayNameAttribute d)
        {
            return d == null ? "" : d._name;
        }

        public override bool IsDefaultAttribute()
        {
            return false;
        }

    }

    /// <summary>
    /// Used to specify the icon that will displayed for the class
    /// </summary>
    class IconAttribute : Attribute
    {
        private string _path;

        public IconAttribute(string path)
        {
            _path = path;
        }

        public static explicit operator string(IconAttribute i)
        {
            return i == null ? "" : i._path;
        }

        public override bool IsDefaultAttribute()
        {
            return false;
        }
    }

    /// <summary>
    /// Mark classes with this attribute to hide in the UI
    /// </summary>
    class VisibleAttribute : Attribute
    {
        private bool _visible = false;

        public VisibleAttribute(bool Visible)
        {
            _visible = Visible;
        }
        
        public static explicit operator bool(VisibleAttribute v)
        {
            return v == null ? true : v._visible;
        }

        public override bool IsDefaultAttribute()
        {
            return false;
        }

    }

    /// <summary>
    /// Used to attach a widget to a category or subcategories in the menu
    /// </summary>
    class SubCategoryAttribute : Attribute, IEnumerable<string>
    {

        private List<string> _categories;
        
        public SubCategoryAttribute(params string[] categories)
        {
            _categories = new List<string>(categories);
        }

        public IEnumerator<string> GetEnumerator()
        {
            return _categories.GetEnumerator();
        }

        public override bool IsDefaultAttribute()
        {
            return false;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _categories.GetEnumerator();
        }
    }
        
    /// <summary>
    /// 
    /// </summary>
    public class KeyItemTitleList : Dictionary<string, uint?> {}
        
    /// <summary>
    /// The VisualizationControl base class, inherited by all visualization controls
    /// </summary>
    [Icon("../icons/application_view_gallery.png")]
    public class VisualizationControl : ContentControl, IDisposable, IReload, INotifyPropertyChanged
    {

        /// <summary>
        /// <see cref="IReload.CancellationTokenSource"/>
        /// </summary>
        public CancellationTokenSource CancellationTokenSource
        {
            get { return (CancellationTokenSource)GetValue(CancellationTokenSourceProperty); }
            set { SetValue(CancellationTokenSourceProperty, value); }
        }
        public static readonly DependencyProperty CancellationTokenSourceProperty =
            DependencyProperty.Register("CancellationTokenSource", typeof(CancellationTokenSource), typeof(VisualizationControl), new PropertyMetadata(new CancellationTokenSource()));



        #region Static Properties and Methods

        /// <summary>
        /// This class is used to hold information about the types that inherits from VisualizationControl
        /// </summary>
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

            public override string ToString()
            {
                return DisplayName ?? Type.Name;
            }
        }
        /// <summary>
        /// Lists information about the types that inherits from VisualizationControl
        /// </summary>
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

                    VisibleAttribute v = info.GetCustomAttribute<VisibleAttribute>();

                    return (bool)v;

                })
                .Select(info =>
                {

                    DisplayNameAttribute DisplayName = info.GetCustomAttribute<DisplayNameAttribute>();
                    IconAttribute Icon = info.GetCustomAttribute<IconAttribute>();                                     

                    return new InheritorInfo(
                        DisplayName == null ? "" : (string)DisplayName,
                        Icon == null ? "" : (string)Icon,
                        info
                    );

                }).ToList();
            }
        }

        #endregion

        #region INotifyPropertyChanged

        /// <summary>
        /// <see cref="INotifyPropertyChanged.PropertyChanged"/>
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Triggers the <see cref="INotifyPropertyChanged.PropertyChanged"/> event
        /// </summary>
        /// <param name="propertyName"></param>
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

        /// <summary>
        /// <see cref="IReload.Status"/>
        /// </summary>
        public ReloadStatus Status
        {
            get
            {
                return (ReloadStatus)GetValue(StatusProperty);
            }
            set
            {
                SetValue(StatusProperty, value);
            }
        }

        #region Dependency Properties

        /// <summary>
        /// The text to show in the status bar of the widget
        /// </summary>
        public string StatusBarText
        {
            get { return (string)GetValue(StatusBarTextProperty); }
            set { SetValue(StatusBarTextProperty, value); }
        }
        public static readonly DependencyProperty StatusBarTextProperty =
            DependencyProperty.Register("StatusBarText", typeof(string), typeof(VisualizationControl), new PropertyMetadata(""));



        /// <summary>
        /// Identifies the <see cref="Status"/> dependency property
        /// </summary>
        public static readonly DependencyProperty StatusProperty =
            DependencyProperty.Register("Status", typeof(ReloadStatus), typeof(VisualizationControl), new PropertyMetadata(ReloadStatus.Idle));
              
        /// <summary>
        /// The title, which can be show at the top of the control
        /// </summary>
        public string Title
        {
            get {
                return (string)GetValue(TitleProperty);
            }
            set {
                SetValue(TitleProperty, value);
            }
        }
        /// <summary>
        /// Identifies the <see cref="Title"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(VisualizationControl), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, (sender, e) =>
            {
                VisualizationControl control = (VisualizationControl)sender;

                if (String.IsNullOrEmpty((string)e.NewValue))
                {
                    control.Title = (string)e.OldValue;
                }

            }));



        /// <summary>
        /// The subtitle, which can be display together with the <see cref="Title"/>  at the top of the control
        /// </summary>
        public string SubTitle
        {
            get { return (string)GetValue(SubTitleProperty); }
            set { SetValue(SubTitleProperty, value); }
        }
        /// <summary>
        /// Identifies the <see cref="SubTitle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SubTitleProperty =
            DependencyProperty.Register("SubTitle", typeof(string), typeof(VisualizationControl), new PropertyMetadata(null));


        /// <summary>
        /// The font size of the <see cref="Title"/>
        /// </summary>
        public double TitleFontSize
        {
            get { return (double)GetValue(TitleFontSizeProperty); }
            set { SetValue(TitleFontSizeProperty, value); }
        }
        /// <summary>
        /// Identifies the <see cref="TitleFontSize"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TitleFontSizeProperty =
            DependencyProperty.Register("TitleFontSize", typeof(double), typeof(VisualizationControl), new PropertyMetadata(14D));




        /// <summary>
        /// The font size of the <see cref="SubTitle"/>
        /// </summary>
        public double SubTitleFontSize
        {
            get { return (double)GetValue(SubTitleFontSizeProperty); }
            set { SetValue(SubTitleFontSizeProperty, value); }
        }
        /// <summary>
        /// Identifies the <see cref="SubTitleFontSize"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SubTitleFontSizeProperty =
            DependencyProperty.Register("SubTitleFontSize", typeof(double), typeof(VisualizationControl), new PropertyMetadata(12D));



        /// <summary>
        /// Sets the font weight of the <see cref="Title"/> and <see cref="SubTitle"/> that can be displayed at the top of the control.
        /// </summary>
        public FontWeight TitleFontWeight
        {
            get { return (FontWeight)GetValue(TitleFontWeightProperty); }
            set { SetValue(TitleFontWeightProperty, value); }
        }
        /// <summary>
        /// Identifies the <see cref="TitleFontWeight"/> property.
        /// </summary>
        public static readonly DependencyProperty TitleFontWeightProperty =
            DependencyProperty.Register("TitleFontWeight", typeof(FontWeight), typeof(VisualizationControl), new PropertyMetadata(FontWeights.Bold));



        /// <summary>
        /// Sets the visibility of the <see cref="Title"/> and <see cref="SubTitle"/> that can be displayed at the top of the control.
        /// </summary>
        public Visibility TitleVisibility
        {
            get { return (Visibility)GetValue(TitleVisibilityProperty); }
            set { SetValue(TitleVisibilityProperty, value); }
        }
        /// <summary>
        /// Identifies the <see cref="TitleVisibility"/> dependency property
        /// </summary>
        public static readonly DependencyProperty TitleVisibilityProperty =
            DependencyProperty.Register("TitleVisibility", typeof(Visibility), typeof(VisualizationControl), new PropertyMetadata(Visibility.Collapsed));
       


        /// <summary>
        /// The owner of this control
        /// </summary>
        public Window Owner
        {
            get { return (Window)GetValue(OwnerProperty); }
            set { SetValue(OwnerProperty, value); }
        }
        /// <summary>
        /// Identifies the <see cref="Owner"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty OwnerProperty =
            DependencyProperty.Register("Owner", typeof(Window), typeof(VisualizationControl), new PropertyMetadata(null));




        #endregion

        /// <summary>
        /// A unique idenfifier for the object
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();
        
        /// <summary>
        /// The index of this object in the parent windows children
        /// </summary>
        public int? Index
        {
            get
            {
                try {
                    return ((VisualizationWindow)Owner).Children.IndexOf(this);
                }
                catch(Exception)
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Holds a list of <typeparamref name="PropertyDefinition"/> objects that will be exposed to the user.
        /// </summary>
        public List<PropertyDefinition> Properties { get; protected set; } = new List<PropertyDefinition>();

        /// <summary>
        /// 
        /// </summary>
        private Dictionary<string, KeyItemW> _groups = new Dictionary<string, KeyItemW>();

        private int _page = 0;

        /// <summary>
        /// 
        /// </summary>
        public KeyItemPathList GroupPaths
        {

            get
            {

                KeyItemPathList list = new KeyItemPathList();

                foreach (var w in _groups)
                {
                    list.Add(new KeyItemPath(w.Value));
                }

                return list;
            }

            set
            {

                foreach (KeyItemPath p in value)
                {

                    if (!_groups.ContainsKey(p.Path))
                        Add(p.ToKeyItemW());
                }

            }

        }

        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<KeyItemW> Groups
        {
            get
            {
                return _groups.Values;
            }
        }
       
        /// <summary>
        /// Holds the key paths.
        /// Used for serialization, need only paths not the whole <typeparamref name="KeyItem"/>
        /// </summary>
        public KeyItemPathList Paths
        {
            get
            {

                KeyItemPathList list = new KeyItemPathList();
                
                foreach(MultiControl c in Controls)
                {
                    if(c.Key.W == null)
                        list.Add(new KeyItemPath(c.Key));
                }

                return list;
            }

            set
            {
                
                foreach (KeyItemPath p in value)
                { 

                    KeyItem k = p.ToKeyItem();

                    if(k.W == null)
                        Add(k);
                }
            }
        }

        private ObservableCollection<MultiControl> _controls = new ObservableCollection<MultiControl>();
        /// <summary>
        /// Holds the controls for each key
        /// </summary>
        public ObservableCollection<MultiControl> Controls
        {
            get
            {
                return _controls;
            }
        }
        
        /// <summary>
        /// The timespan that will be used when loading time data
        /// </summary>
        public TimeSpan Timespan { get; set; } = new TimeSpan(1, 0, 0, 0, 0);

        /// <summary>
        /// The count used when loading index data
        /// </summary>
        public long Count { get; set; }      = 1L;
        /// <summary>
        /// The start index that will be used when loading index data
        /// </summary>
        public long StartIndex { get; set; } = 0L;

        /// <summary>
        /// Defines how data is loaded based on the <typeparamref name="ControlType"/>
        /// </summary>
        [Editor(typeof(StoreEnumEditor), typeof(StoreEnumEditor))]
        public string DataIndexField { get; set; }

        private Enum DataIndexFieldAsEnum
        {
            get
            {
                return (Enum)Enum.Parse(Store.Get(Dispatcher).Index, DataIndexField);
            }
        }

        /// <summary>
        /// The vertical position in the grid (If any)
        /// </summary>
        public int? Row {

            get {
                return (int?)(GetValue(Grid.RowProperty)??0);
            }

            set {
                SetValue(Grid.RowProperty, Math.Max(0, value ?? 0));
            }
        }
        /// <summary>
        /// How many rows to fill in the grid (If any)
        /// </summary>
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
        /// <summary>
        /// The horizontal position in the grid (If any)
        /// </summary>
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
        /// <summary>
        /// How many columns to fill in the grid (If any) 
        /// </summary>
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

        /// <summary>
        /// A thumbnail image of the widget
        /// </summary>
        public BitmapSource Thumbnail
        {

            get
            {
                return this.GetThumbnail();
            }

        }
               

        #region Commands

        /// <summary>
        /// Used to identify the different commands
        /// </summary>
        protected enum CommandType
        {
            /// <summary>
            /// Modify the title position
            /// </summary>
            METRICS = int.MinValue,
            /// <summary>
            /// Shows the properties window
            /// </summary>
            PROPERTIES,
            /// <summary>
            /// Load previous results
            /// </summary>
            PREV,
            /// <summary>
            /// Load next results
            /// </summary>
            NEXT
        }
                
        /// <summary>
        /// A read only wrapper around the <typeparamref name="CommandCollection"/> <see cref="CommandsSource"/>
        /// </summary>
        public ReadOnlyDictionary<string, RelayCommand> Commands
        {
            get
            {
                return new ReadOnlyDictionary<string, RelayCommand>(CommandsSource);
            }
        }

        /// <summary>
        /// Holds the commands for this object
        /// </summary>
        protected CommandCollection CommandsSource = new CommandCollection();
        
        /// <summary>
        /// Used to execute commands based on <typeparamref name="CommandType"/>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="parameter"></param>
        protected virtual void ExecuteCommand(object sender, object parameter)
        {

            CommandType cmd = (CommandType)((RelayCommand)sender).Arguments[0].Value;

            Window window = null;
            PropertiesWindow grid = null;

            switch (cmd)
            {

                case CommandType.METRICS:

                    window = new Dialogs.KeyEditor(this);
                    window.Title += String.Format(" [{0}]", Title);
                    window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    window.Owner = this.Owner;
                    window.Width = Math.Sqrt(this.Owner.Width * this.Owner.Height);
                    window.Height = window.Width;
                    
                    window.ShowDialog();

                    this.OnReload(this);

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

                case CommandType.METRICS:

                    break;

                case CommandType.PROPERTIES:
                    
                    window = ((grid = new PropertiesWindow(this, Properties.ToArray())
                    {
                        Title = String.Format("Properties [{0}]", this.Title),
                        ShowInTaskbar = false,
                        Owner = this.Owner,
                        Width = Math.Sqrt(this.Owner.Width * this.Owner.Height)

                    }));
                    
                    window.Height = window.Width;
                    window.ShowDialog();
                    
                    break;
            }

            Refresh();    

        }
        
        #endregion

        protected Func<object, object, bool> canExecute = delegate { return true; };
        
        /// <summary>
        /// The default constructor
        /// </summary>
        public VisualizationControl()
        {

            DataIndexField = Store.Get(Dispatcher).Default.ToString();

            DataContext = this;
            Template    = (ControlTemplate)FindResource("VisualizationControlTemplate");
            Title       = String.Format("<{0}> [{1}]", GetType().Name, Id);
            Margin      = new Thickness(5);
            AllowDrop   = true;

            #region Commands
            
            CommandsSource.Add("Metrics", new RelayCommand(ExecuteCommand, canExecute, CommandType.METRICS));
            CommandsSource.Add("Properties", new RelayCommand(ExecuteCommand, canExecute, CommandType.PROPERTIES));
            CommandsSource.Add("Previous", new RelayCommand(ExecuteCommand, canExecute, CommandType.PREV));
            CommandsSource.Add("Next", new RelayCommand(ExecuteCommand, canExecute, CommandType.NEXT));

            #endregion

            #region PropertyDefinitions

                Properties.Add(new PropertyDefinition()
                {
                    Category = "Layout",
                    TargetProperties = new List<object>(new string[] { "Row", "RowSpan", "Column", "ColumnSpan", "Margin", "MinWidth", "MinHeight", "MaxWidth", "MaxHeight" })
                });

                Properties.Add(new PropertyDefinition()
                { 
                    Category = "Controls",
                    TargetProperties = new List<object>(new string[] { "Timespan", "Count", "StartIndex", "DataIndexField" })
                });

                Properties.Add(new PropertyDefinition()
                {
                    Category = "Common",
                    TargetProperties = new List<object>(new string[] { "FontStyle", "FontFamily", "FontWeight", "FontSize", "BorderThickness" })
                });

                Properties.Add(new PropertyDefinition()
                {
                    Category = "Title",
                    TargetProperties = new List<object>(new string[] { "Title", "SubTitle", "TitleVisibility", "TitleFontSize", "SubTitleFontSize", "TitleFontWeight"  })
                }); 

                Properties.Add(new PropertyDefinition()
                {
                    IsExpandable = true,
                    Category = "Colors",
                    TargetProperties = new List<object>(new string[] { "Background", "Foreground", "BorderBrush", "Opacity" })
                });

                Properties.Add(new PropertyDefinition()
                {
                    IsExpandable = true,
                    Category = "Defaults",
                    TargetProperties = new List<object>(new string[] { "Defaults" })
                });


            #endregion

            #region UserCommands

            RegisterUserCommand("Properties", CommandsSource["Properties"]);
            RegisterUserCommand();
            RegisterUserCommand("Metrics", CommandsSource["Metrics"]);

            #endregion

            ContextMenu = new ContextMenu();
            ContextMenu.ContextMenuOpening += delegate
            {
                ContextMenu.ItemsSource = MenuItems;
            };
                      

            this.SizeChanged += delegate {
                if (_refreshOperation == null)
                    _refreshOperation = Dispatcher.InvokeAsync(Refresh, DispatcherPriority.Background);
            };

            this.Drop += VisualizationControl_Drop;            

        }

        protected DispatcherOperation _refreshOperation = null;

        /// <summary>
        /// Drop keys onto the widget
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void VisualizationControl_Drop(object sender, DragEventArgs e)
        {

            if (e.Data.GetDataPresent(DataFormats.StringFormat))
            {

                string path = (string)e.Data.GetData(DataFormats.StringFormat);

                if(path != null)
                {
                   Add(KeyItem.Create(path));
                }
               
            }
        }

        /// <summary>
        /// Holds the Usercommands added with <see cref="RegisterUserCommand" />
        /// </summary>
        protected Dictionary<string, object> _userCommands = new Dictionary<string, object>();

        /// <summary>
        /// Returns items based on <see cref="_userCommands"/> that can be used in menus
        /// </summary>
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

        /// <summary>
        /// Register a command that will show up in the menus
        /// When called with no parameters, a seperator is added in the menus
        /// </summary>
        /// <param name="title">The header that will be displayed in the menuitem</param>
        /// <param name="command">The command that will be executed. Normally a <see cref="RelayCommand"/></param>
        public void RegisterUserCommand(string title = null, ICommand command = null)
        {
            if (title != null && command != null)
                _userCommands.Add(title, command);
            else
                _userCommands.Add(Guid.NewGuid().ToString(), null);

            OnPropertyChanged("MenuItems");
        }

        /// <summary>
        /// Refresh the rendering, no data should be updated by this method.
        /// </summary>
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

            UpdateLayout();

            _refreshOperation = null;

        }

        /// <summary>
        /// Clean up
        /// </summary>
        public virtual void Dispose()
        {
            foreach(MultiControl m in _controls.ToArray())
            {
                Remove(m.Key);
                m.Dispose();
            }

            _controls.Clear();
           
        }

        /// <summary>
        /// Helper method to easily get the controls for a key
        /// </summary>
        /// <param name="key">The key</param>
        /// <returns>The <see cref="MultiControl"/> if any</returns>
        public MultiControl GetControl(KeyItem key)
        {
            return (from s in _controls where s.Key == key select s).ElementAtOrDefault(0);
        }

        /// <summary>
        /// Overridden so that the content is not serialized when the object is saved to disk
        /// </summary>
        /// <returns><c>false</c></returns>
        public override bool ShouldSerializeContent()
        {
            return false;
        }

        /// <summary>
        /// Used to disable serialization of some properties that are unwanted in the serialized document when save to disk
        /// </summary>
        /// <param name="dp">The property</param>
        /// <returns><c>false</c> if the property should not be serialized</returns>
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
                ContentProperty,
                NameProperty,
                TemplateProperty,
                IsEnabledProperty,
                AllowDropProperty,
                VisibilityProperty,
                StatusProperty,
                CancellationTokenSourceProperty
            };

            foreach (DependencyProperty p in properties)
            {
                if (dp == p) return false;
            }
            
            return base.ShouldSerializeProperty(dp);
        }

        /// <summary>
        /// Add wildcard item
        /// </summary>
        /// <param name="key"></param>
        public bool[] Add(KeyItemW key)
        {

            bool[] results = new bool[key.Children.Count];
            int i = 0;

            if(!_groups.ContainsKey(key.StaticPath) && key.HasWildcards)
            {
                key.Children.CollectionChanged += Key_Children_CollectionChanged;

                foreach (KeyItem k in key.Children)
                    results[i++] = Add(k);

                key.AutoRefresh = key.HasWildcards;

                if (key.HasWildcards)
                    _groups.Add(key.StaticPath, key);
            }
            else if(!key.HasWildcards)
            {
                foreach (KeyItem k in key.Children)
                    Add(k);
            }

            return results;

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        public virtual bool Add(IKeyItem key)
        {
            if (key is KeyItem)
                return Add((KeyItem)key);
            else if (key is KeyItemW)
                return Add((KeyItemW)key).Length > 1;

            return false;
        }

        /// <summary>
        /// Remove wildcard item
        /// </summary>
        /// <param name="key"></param>
        public virtual bool Remove(KeyItemW key)
        {

            key.AutoRefresh = false;

            foreach (KeyItem k in key.Children)
                Remove(k);

            key.Children.CollectionChanged -= Key_Children_CollectionChanged;

            return _groups.Remove(key.StaticPath);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        public virtual bool Remove(IKeyItem key)
        {
            
            if (key is KeyItemW)
                return Remove((KeyItemW)key);
            else if (key is KeyItem)
                return Remove((KeyItem)key);

            return false;
        }

        /// <summary>
        /// Add a key to this control. So that data is loaded for this key.
        /// </summary>
        /// <param name="key">The key to load</param>
        /// <param name="collection">A collection for the data. Used to share collection between objects.</param>
        /// <returns>The MultiControl that is created for the key</returns>
        public virtual bool Add(KeyItem key, ObservableCollection<Entry> collection = null)
        {

            if (key == null || key.Type == null) return false;

            MultiControl control = GetControl(key);

            if (control == null)
            {
                
                control = new MultiControl(key, null, collection);
                _controls.Add(control);
                control.Get(DataIndexFieldAsEnum).Page = _page;


            }
            else
                return false;
            
            return true;

        }
        
        /// <summary>
        /// Removes the key, so that it will no longer be associated with this control.
        /// </summary>
        /// <param name="key">The KeyItem to remove.</param>
        public virtual bool Remove(KeyItem key)
        {
            
            foreach (MultiControl m in (from s in Controls.ToArray() where s.Key == key || (s.Key.Parent != null && s.Key.Parent == key) select s))
            {
                Controls.Remove(m);
                m.Dispose();
            }
                        
            return true;
        }


        /// <summary>
        /// Called when the keys are added or removed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Key_Children_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            
            if(e.OldItems != null)
                foreach (KeyItem k in e.OldItems)
                {
                    Remove(k);

                }

            if (e.NewItems != null)
                foreach (KeyItem k in e.NewItems)
                {
                    Add(k);
                }

            if (_refreshOperation == null && (e.OldItems != null || e.NewItems != null))
            {
                _refreshOperation = Dispatcher.InvokeAsync(Refresh, DispatcherPriority.Background);
                this.OnReload(this);
            }

        }

        /// <summary>
        /// IReload.Reload data and keys
        /// </summary>
        public virtual void Reload()
        {
            
            object Start = null;
            object Count = null;
            
            switch(DataIndexFieldAsEnum.GetType().FullName)
            {

                case "PSM.Stores.Dummy+IndexType":

                    Start = StartIndex;
                    Count = this.Count;

                    break;

                case "PSM.Stores.DB+IndexType":

                    switch((DB.IndexType)DataIndexFieldAsEnum)
                    {
                        
                        case DB.IndexType.Descending:

                            Start = StartIndex;
                            Count = this.Count;

                            break;

                        case DB.IndexType.Timestamp:

                            Count = Timespan;

                            break;

                        default:
                            break;
                    }

                    break;

                case "PSM.Stores.Advantage+IndexType":

                    switch ((Advantage.IndexType)DataIndexFieldAsEnum)
                    {
                        case Advantage.IndexType.Descending:
                        case Advantage.IndexType.Depth:

                            Start = StartIndex;
                            Count = this.Count;

                            break;

                        case Advantage.IndexType.Time:

                            Count = Timespan;
                            break;

                        default:
                            break;
                    }

                    break;

                default:

                    throw new Exception("Unsupported Index Type");

            }

            foreach (KeyItemW w in _groups.Select((c) => { return c.Value; }))
            {
                w.Reload();
            }

            foreach (MultiControl m in _controls)
            {

                Controls control = m.Get(DataIndexFieldAsEnum, Count);

                control.PropertyChanged -= Control_PropertyChanged;
                control.PropertyChanged += Control_PropertyChanged;

                KeyItem key = m.Key;

                m.Reload();

            }            

            if (_refreshOperation == null)
                _refreshOperation = Dispatcher.InvokeAsync(Refresh, DispatcherPriority.Background);

        }

        private void Control_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {

            Dispatcher.Invoke(delegate
            {
                if (e.PropertyName == "Status")
                {

                    foreach (MultiControl controls in Controls)
                    {

                        IReload reloadable = controls.Get(DataIndexFieldAsEnum);

                        if (reloadable.Status != ReloadStatus.Idle)
                        {
                            Status = reloadable.Status;
                            return;
                        }

                    }

                    Status = ReloadStatus.Idle;
                }
            });



        }

        /// <summary>
        /// Loads the next results
        /// </summary>
        /// <returns><c>false</c> if there is more data to be loaded in this direction</returns>
        public virtual bool Next()
        {
            bool yn = false;

            foreach (MultiControl control in _controls)
            {
                Controls ctrl = control.Get(DataIndexFieldAsEnum);
                yn |= ctrl.Next();
                _page = ctrl.Page;
            }

            return yn;
        }

        /// <summary>
        /// Loads the previous results
        /// </summary>
        /// <returns><c>false</c> if there is more data to be loaded in this direction</returns>
        public virtual bool Previous()
        {

            bool yn = false;

            foreach (MultiControl control in _controls)
            {
                Controls ctrl = control.Get(DataIndexFieldAsEnum);
                yn |= control.Get(DataIndexFieldAsEnum).Previous();
                _page = ctrl.Page;
            }

            return yn;
        }

        public virtual void Reset()
        {
            foreach (MultiControl control in _controls)
            {
                Controls ctrl = control.Get(DataIndexFieldAsEnum);
                control.Reset();
                _page = ctrl.Page;
            }
        }
                
        /// <summary>
        /// Overrides what should be displayed if the object is converted to text.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return String.IsNullOrEmpty(Title) ? String.Format("<{0}> [{1}]", GetType().Name, Id) : Title;
        }

        
    }
}
