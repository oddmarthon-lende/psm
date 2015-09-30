/// <copyright file="base.cs" company="Baker Hughes Incorporated">
/// Copyright (c) 2015 All Rights Reserved
/// </copyright>
/// <author>Odd Marthon Lende</author>
/// <summary>The base for all visualization controls</summary>
/// 

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
using System.Collections;
using System.Windows.Media.Imaging;
using System.Threading;
using System.Windows.Threading;

namespace PSMViewer.Visualizations
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
    /// A wrapper class around List<string>.
    /// Used to hold the key paths when the VisualizationControl is serialized to XAML
    /// </summary>
    public class KeyItemPathList : List<string> {}

    /// <summary>
    /// The VisualizationControl base class, inherited by all visualization controls
    /// </summary>
    [Icon("../icons/application_view_gallery.png")]
    public class VisualizationControl : ContentControl, IDisposable, IReload, INotifyPropertyChanged, IUndo
    {

        /// <summary>
        /// <see cref="IReload.Cancel"/>
        /// </summary>
        public CancellationTokenSource Cancel { get; protected set; } = new CancellationTokenSource();

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

        #region Dependency Properties


        /// <summary>
        /// <see cref="IReload.Status"/>
        /// </summary>
        public ReloadStatus Status
        {
            get {
                return (ReloadStatus)GetValue(StatusProperty);
            }
            set {
                SetValue(StatusProperty, value);
            }
        }
        /// <summary>
        /// Identifies the <see cref="Status"/> dependency property
        /// </summary>
        public static readonly DependencyProperty StatusProperty =
            DependencyProperty.Register("Status", typeof(ReloadStatus), typeof(VisualizationControl), new PropertyMetadata(ReloadStatus.Idle));
        

        /// <summary>
        /// The visibility of the right and left navigation arrows that are overlayed the control
        /// </summary>
        public Visibility HorizontalArrowsVisibility
        {
            get { return (Visibility)GetValue(HorizontalArrowsVisibilityProperty); }
            set {
                SetValue(HorizontalArrowsVisibilityProperty, value);
            }
        }
        /// <summary>
        /// Identifies the <see cref="HorizontalArrowsVisibility"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HorizontalArrowsVisibilityProperty =
            DependencyProperty.Register("HorizontalArrowsVisibility", typeof(Visibility), typeof(VisualizationControl), new FrameworkPropertyMetadata(Visibility.Collapsed, FrameworkPropertyMetadataOptions.AffectsRender, (sender, e) =>
            {

                VisualizationControl w = (VisualizationControl)sender;

                if (((Visibility)e.NewValue) != Visibility.Visible && ((Visibility)e.OldValue) == Visibility.Visible)
                    w.PopState();
                else if (((Visibility)e.NewValue) == Visibility.Visible && ((Visibility)e.OldValue) != Visibility.Visible)
                    w.PushState();

            }));



        /// <summary>
        /// The visibility of the up and down navigation arrows that are overlayed the control
        /// </summary>
        public Visibility VerticalArrowsVisibility
        {
            get { return (Visibility)GetValue(VerticalArrowsVisibilityProperty); }
            set { SetValue(VerticalArrowsVisibilityProperty, value); }
        }
        /// <summary>
        /// Identifies the <see cref="VerticalArrowsVisibility"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty VerticalArrowsVisibilityProperty =
            DependencyProperty.Register("VerticalArrowsVisibility", typeof(Visibility), typeof(VisualizationControl), new PropertyMetadata(Visibility.Collapsed));
        
        
                
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
        protected List<PropertyDefinition> Properties { get; set; } = new List<PropertyDefinition>();
        
        private KeyItemPathList keys = new KeyItemPathList();
        /// <summary>
        /// Holds the key paths.
        /// Used for serialization, need only paths not the whole <typeparamref name="KeyItem"/>
        /// </summary>
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
        /// <summary>
        /// Holds the controls for each key
        /// </summary>
        public ObservableCollection<MultiControl> Controls
        {
            get
            {
                return controls;
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
        public ControlType SelectedControlType { get; set; } = ControlType.Index;

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
        /// Defines how data can be loaded
        /// </summary>
        public enum ControlType
        {
            /// <summary>
            /// Load data using time as index
            /// </summary>
            Time,
            /// <summary>
            /// Loads data using index as index
            /// </summary>
            Index
        }

        /// <summary>
        /// A thumbnail of the controls contents
        /// </summary>
        public BitmapSource Thumbnail
        {

            get
            {
                return this.GetThumbnail();
            }

        }

        /// <summary>
        /// A multicontrol can handle the different <typeparamref name="ControlType"/> in one object.
        /// Each key gets a <typeparamref name="MultiControl"/> when added to the <typeparamref name="VisualizationControl"/>
        /// </summary>
        public class MultiControl : IDisposable, IUndo
        {
            public KeyItem Key { get; private set; }
            public ObservableCollection<EntryItem> Entries { get; set; }
            public event DataChangedEventHandler DataChanged;

            private Dictionary<ControlType, Controls> Controls = new Dictionary<ControlType, ViewModels.Controls>();

            /// <summary>
            /// The constructor
            /// </summary>
            /// <param name="key">The <typeparamref name="KeyItem"/> this <typeparamref name="MultiControl"/> is for</param>
            /// <param name="Entries">An alternate collection for the data. Used to share a collection between different controls.</param>
            public MultiControl(KeyItem key, LoadHandler load = null, ObservableCollection<EntryItem> Entries = null)
            {

                this.Key = key;
                this.Entries = Entries ?? new ObservableCollection<EntryItem>();

                Controls.Add(ControlType.Index, new Controls<long, long>(this.Entries, 0, 1) { Selected = key });
                Controls.Add(ControlType.Time, new Controls<DateTime, TimeSpan>(this.Entries, null, new TimeSpan()) { Selected = key });

                Stack = new Stack<Dictionary<ControlType, ViewModels.Controls>>();

                foreach (var pair in Controls)
                {

                    if (load != null)
                        pair.Value.Load += load;

                    pair.Value.DataChanged += Value_DataChanged;
                }

            }

            private Stack<Dictionary<ControlType, ViewModels.Controls>> Stack;

            /// <summary>
            /// Make a copy of the current state and push it to the stack
            /// </summary>
            public void PushState()
            {

                Dictionary<ControlType, Controls> c = new Dictionary<ControlType, ViewModels.Controls>();

                Stack.Push(c);

                foreach (KeyValuePair<ControlType, Controls> pair in Controls)
                {
                    c[pair.Key] = (Controls)Activator.CreateInstance(pair.Value.GetType(), Controls[pair.Key]);
                    c[pair.Key].DataChanged += Value_DataChanged;
                }
            }

            /// <summary>
            /// Pop off the stack and restore the saved state
            /// </summary>
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

            /// <summary>
            /// Called when the data has changed for a control type
            /// </summary>
            /// <param name="sender"></param>
            private void Value_DataChanged(object sender)
            {
                if (DataChanged != null)
                    DataChanged(sender);
            }

            /// <summary>
            /// Gets the controls
            /// </summary>
            /// <param name="type"></param>
            /// <param name="Start"></param>
            /// <param name="Count"></param>
            /// <returns>The controls</returns>
            public Controls Get(ControlType type, object Start = null, object Count = null)
            {

                Controls c = Controls[type];

                if (c != null)
                {

                    c.Activate(this);

                    if (Start != null)
                        c.Start = Start;

                    if (Count != null)
                        c.Count = Count;
                }

                return c;
            }

            /// <summary>
            /// Cleans up and releases any resource used by this object.
            /// </summary>
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

        #region Commands

        /// <summary>
        /// Used to identify the different commands
        /// </summary>
        protected enum CommandType
        {
            /// <summary>
            /// Add a key to visualize its data
            /// </summary>
            ADD_KEY = int.MinValue,
            /// <summary>
            /// Remove a previously added key
            /// </summary>
            REMOVE_KEY,
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
            NEXT,
            DOWN,
            UP
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

            Tree tree = null;
            Window window = null;
            
            switch (cmd)
            {

                case CommandType.ADD_KEY:
                case CommandType.REMOVE_KEY:

                    tree = new Tree();
                    window = tree.Window;

                    window.WindowStyle = WindowStyle.ToolWindow;
                    window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    window.Owner = this.Owner;
                    window.Width = Math.Sqrt(this.Owner.Width * this.Owner.Height);
                    window.Height = window.Width;

                    break;

            }

            switch (cmd)
            {

                case CommandType.DOWN:

                    Down();
                    return;

                case CommandType.UP:

                    Up();
                    return;

                case CommandType.NEXT:

                    Previous();
                    return;

                case CommandType.PREV:
                                        
                    Next();
                    return;

                case CommandType.ADD_KEY:

                    if(parameter != null && parameter is KeyItem)
                    {
                        Add((KeyItem)parameter);
                        break;
                    }

                    window.Title = String.Format("Add Key [{0}]", Title);

                    this.OnReload(tree);

                    tree.Window.ShowDialog();

                    if(tree.SelectedValue != null)
                        Add((KeyItem)tree.SelectedValue);
                                        
                    break;

                case CommandType.REMOVE_KEY:

                    window.Title = String.Format("Remove Key [{0}]", Title);

                    ((TreeViewItem)tree.Items[0]).ItemsSource = Keys.Select(path => { return KeyItem.CreateFromPath(path); });

                    tree.Window.ShowDialog();

                    if (tree.Key != null)
                        Remove(tree.Key);

                    break;

                case CommandType.PROPERTIES:

                    PushState();                    
                    
                    window = (new PropertiesWindow(this, Properties.ToArray())
                    {
                        Title = String.Format("Properties [{0}]", this.Title),
                        ShowInTaskbar = false,
                        Owner = this.Owner,
                        Width = Math.Sqrt(this.Owner.Width * this.Owner.Height)
                    });

                    window.Height = window.Width;

                    window.ShowDialog();
                    
                    break;
            }

            this.OnReload(this);

        }
        
        #endregion

        protected Func<object, object, bool> canExecute = delegate { return true; };
        
        /// <summary>
        /// The default constructor
        /// </summary>
        public VisualizationControl()
        {
            
            DataContext = this;
            Template    = (ControlTemplate)FindResource("VisualizationControlTemplate");
            Title       = String.Format("<{0}> [{1}]", GetType().Name, Id);
            Margin      = new Thickness(5);
            AllowDrop   = true;

            #region Commands

            CommandsSource.Add("Add", new RelayCommand(ExecuteCommand, canExecute, CommandType.ADD_KEY));
            CommandsSource.Add("Remove", new RelayCommand(ExecuteCommand, canExecute, CommandType.REMOVE_KEY));
            CommandsSource.Add("Properties", new RelayCommand(ExecuteCommand, canExecute, CommandType.PROPERTIES));
            CommandsSource.Add("Previous", new RelayCommand(ExecuteCommand, canExecute, CommandType.PREV));
            CommandsSource.Add("Next", new RelayCommand(ExecuteCommand, canExecute, CommandType.NEXT));
            CommandsSource.Add("Down", new RelayCommand(ExecuteCommand, canExecute, CommandType.DOWN));
            CommandsSource.Add("Up", new RelayCommand(ExecuteCommand, canExecute, CommandType.UP));

            #endregion

            #region PropertyDefinitions

            Properties.Add(new PropertyDefinition()
                {
                    Category = "Layout",
                    TargetProperties = new List<object>(new string[] { "Row", "RowSpan", "Column", "ColumnSpan", "Margin" })
                });

                Properties.Add(new PropertyDefinition()
                { 
                    Category = "Controls",
                    TargetProperties = new List<object>(new string[] { "Timespan", "Count", "StartIndex", "SelectedControlType" })
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
                HorizontalArrowsVisibility = Visibility.Collapsed;
                VerticalArrowsVisibility = Visibility.Collapsed;
            };
                      

            this.SizeChanged += delegate {
                if (RefreshOperation == null)
                    RefreshOperation = Dispatcher.InvokeAsync(Refresh, DispatcherPriority.Background);
            };

            this.Drop += VisualizationControl_Drop;            

        }

        protected DispatcherOperation RefreshOperation = null;

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
                    CommandsSource["Add"].Execute(KeyItem.CreateFromPath(path));
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

            RefreshOperation = null;

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

        /// <summary>
        /// Clean up
        /// </summary>
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

        /// <summary>
        /// Helper method to easily get the controls for a key
        /// </summary>
        /// <param name="key">The key</param>
        /// <returns>The <see cref="MultiControl"/> if any</returns>
        protected MultiControl GetControlsFor(KeyItem key)
        {
            return (from s in controls where s.Key.Path == key.Path select s).ElementAtOrDefault(0);
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
                HorizontalArrowsVisibilityProperty,
                VerticalArrowsVisibilityProperty,
                TemplateProperty,
                IsEnabledProperty,
                AllowDropProperty,
                VisibilityProperty,
                StatusProperty
            };

            foreach (DependencyProperty p in properties)
            {
                if (dp == p) return false;
            }
            
            return base.ShouldSerializeProperty(dp);
        }

        /// <summary>
        /// Add a key to this control. So that data is loaded for this key.
        /// </summary>
        /// <param name="key">The key to load</param>
        /// <param name="collection">A collection for the data. Used to share collection between objects.</param>
        /// <returns>The MultiControl that is created for the key</returns>
        public virtual MultiControl Add(KeyItem key, ObservableCollection<EntryItem> collection = null)
        {

            if (key == null) return null;

            if (key.Type == null)
            {
                Keys.Add(key.Path);

                key.Children.CollectionChanged += Key_Children_CollectionChanged;

                this.OnReload(key);

                return null;
            }

            MultiControl control = GetControlsFor(key);

            if (control == null)
            {

                control = new MultiControl(key, this.OnReload, collection);                
                controls.Add(control);

                if (key.Parent != null && !keys.Contains(key.Parent.Path))
                    keys.Add(key.Path);

            }

            if(RefreshOperation == null)
                RefreshOperation = Dispatcher.InvokeAsync(Refresh, DispatcherPriority.Background);

            return control;

        }

        /// <summary>
        /// Removes the key, so that it will no longer be associated with this control.
        /// </summary>
        /// <param name="key">The KeyItem to remove.</param>
        public virtual void Remove(KeyItem key)
        {

            key.Children.CollectionChanged -= Key_Children_CollectionChanged;

            foreach(MultiControl m in (from s in Controls.ToArray() where s.Key.Path == key.Path || (s.Key.Parent != null && s.Key.Parent.Path == key.Path) select s))
            {
                Controls.Remove(m);
                m.Dispose();
            }

            Keys.Remove(key.Path);

            if (RefreshOperation == null)
                RefreshOperation = Dispatcher.InvokeAsync(Refresh, DispatcherPriority.Background);

        }


        /// <summary>
        /// Called when the keys are added or removed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void Key_Children_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
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

            if (RefreshOperation == null)
                RefreshOperation = Dispatcher.InvokeAsync(Refresh, DispatcherPriority.Background);

        }

        /// <summary>
        /// IReload.Reload data and keys
        /// </summary>
        public virtual void Reload()
        {
            
            HorizontalArrowsVisibility = Visibility.Collapsed;

            object Start = null;
            object Count = null;

            switch (SelectedControlType)
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

                control.PropertyChanged -= Control_PropertyChanged;
                control.PropertyChanged += Control_PropertyChanged;

                KeyItem key = m.Key;

                this.OnReload(key);
                this.OnReload(control);

            }

            if (RefreshOperation == null)
                RefreshOperation = Dispatcher.InvokeAsync(Refresh, DispatcherPriority.Background);

        }

        private void Control_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            
            if (e.PropertyName == "Status")
            {

                foreach (MultiControl controls in Controls)
                {

                    IReload reloadable = controls.Get(SelectedControlType);

                    if (reloadable.Status != ReloadStatus.Idle)
                    {
                        Status = reloadable.Status;
                        return;
                    }

                }

                Status = ReloadStatus.Idle;
            }
                

            
        }

        /// <summary>
        /// Loads the next results
        /// </summary>
        /// <returns><c>false</c> if there is more data to be loaded in this direction</returns>
        public virtual bool Next()
        {
            bool yn = false;

            foreach (MultiControl control in controls)
            {
                yn |= control.Get(SelectedControlType).Next();
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

            foreach (MultiControl control in controls)
            {
                yn |= control.Get(SelectedControlType).Previous();
            }

            return yn;
        }

        /// <summary>
        /// Called when the Up arrow is clicked
        /// </summary>
        public virtual void Up() {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Called when the Down arrow is clicked
        /// </summary>
        public virtual void Down() {
            throw new NotImplementedException();
        }


        /// <summary>
        /// Save state.
        /// </summary>
        /// 
        public virtual void PushState()
        {
            
            foreach (MultiControl control in controls)
            {
                control.PushState();
            }

            UndoExtension.PushState(this);
        }

        /// <summary>
        /// Restore state.
        /// </summary>
        ///        
        public virtual void PopState()
        {
            
            foreach (MultiControl control in controls)
            {
                control.PopState();
            }

            UndoExtension.PopState(this, ShouldSerializeProperty);

            if (RefreshOperation == null)
                RefreshOperation = Dispatcher.InvokeAsync(Refresh, DispatcherPriority.Background);

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
