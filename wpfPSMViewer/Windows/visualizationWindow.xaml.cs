﻿/// <copyright file="visualizationwindow.cs" company="Baker Hughes Incorporated">
/// Copyright (c) 2015 All Rights Reserved
/// </copyright>
/// <author>Odd Marthon Lende</author>
/// <summary>Code behind for the Visualization\Chart Windows </summary>
/// 
using System;
using System.Windows;
using PSMViewer.Visualizations;
using System.Collections.Specialized;
using PSMViewer.ViewModels;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Windows.Input;
using System.Windows.Controls;
using Microsoft.Win32;
using Xceed.Wpf.Toolkit.PropertyGrid;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.IO.IsolatedStorage;
using System.IO;
using System.Drawing;
using System.Windows.Media.Imaging;
using System.Drawing.Imaging;
using System.Windows.Media;
using System.Windows.Data;
using PSMViewer.Utilities;
using System.Threading;

namespace PSMViewer
{

    public class VisualizationControlList : ObservableCollection<VisualizationControl> { }

    public class RowDefinition
    {
        public System.Windows.Controls.RowDefinition Source { get; private set; }

        public GridLength Height
        {

            get
            {
                return Source.Height;
            }

            set
            {
                Source.Height = value;
            }
        }

        public RowDefinition() : this(null) { }

        public RowDefinition(System.Windows.Controls.RowDefinition Source = null)
        {
            this.Source = Source??new System.Windows.Controls.RowDefinition();
        }

        public static explicit operator RowDefinition(System.Windows.Controls.RowDefinition Source)
        {
            return new RowDefinition(Source);
        }

        public static explicit operator System.Windows.Controls.RowDefinition(RowDefinition Wrapped)
        {
            return Wrapped.Source;
        }
    }

    public class ColumnDefinition
    {
        public System.Windows.Controls.ColumnDefinition Source { get; private set; }

        public GridLength Width {

            get
            {
                return Source.Width;
            }

            set
            {
                Source.Width = value;
            }
        }

        public ColumnDefinition() : this(null) { }

        public ColumnDefinition(System.Windows.Controls.ColumnDefinition Source = null)
        {
            this.Source = Source??new System.Windows.Controls.ColumnDefinition();
        }

        public static explicit operator ColumnDefinition(System.Windows.Controls.ColumnDefinition Source)
        {
            return new ColumnDefinition(Source);
        }

        public static explicit operator System.Windows.Controls.ColumnDefinition(ColumnDefinition Wrapped)
        {
            return Wrapped.Source;
        }
    }

    public class RowDefinitionList        : ObservableCollection<RowDefinition> { }

    public class ColumnDefinitionList     : ObservableCollection<ColumnDefinition> { }
        
    /// <summary>
    /// A window that can contain many <see cref="VisualizationControl"/>
    /// </summary>
    public partial class VisualizationWindow : Window, IReload, INotifyPropertyChanged, IUndo
    {
        
        private CancellationTokenSource _c = new CancellationTokenSource();
        public CancellationTokenSource Cancel
        {
            get
            {
                return _c;
            }
        }

        #region Properties              

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

        public Guid Id { get; set; } = Guid.NewGuid();

        private VisualizationControlList _children = null;
        public VisualizationControlList Children {

            get {
                return _children;
            }

            set
            {
                if(_children != null)
                    _children.CollectionChanged -= Children_CollectionChanged;

                _children = value;

                _children.CollectionChanged += Children_CollectionChanged;
            }
        }
        
        private RowDefinitionList _rowdefs = null;
        public RowDefinitionList RowDefinitions
        {

            get
            {
                return _rowdefs;
            }
            set
            {
                if(_rowdefs != null)
                    _rowdefs.CollectionChanged -= Children_CollectionChanged;

                _rowdefs = value;

                _rowdefs.CollectionChanged += Children_CollectionChanged;
            }
        }

        private ColumnDefinitionList _coldefs = null;
        public ColumnDefinitionList ColumnDefinitions {

            get
            {
                return _coldefs;
            }
            set
            {
                if(_coldefs != null)
                    _coldefs.CollectionChanged -= Children_CollectionChanged;

                _coldefs = value;

                _coldefs.CollectionChanged += Children_CollectionChanged;
            }

        }



        public ReloadStatus Status
        {
            get { return (ReloadStatus)GetValue(StatusProperty); }
            set { SetValue(StatusProperty, value); }
        }
        public static readonly DependencyProperty StatusProperty =
            DependencyProperty.Register("Status", typeof(ReloadStatus), typeof(VisualizationWindow), new PropertyMetadata(ReloadStatus.Idle));

        

        public Visibility ControlsVisibility
        {
            get { return (Visibility)GetValue(ControlsVisibilityProperty); }
            set { SetValue(ControlsVisibilityProperty, value); }
        }
        public static readonly DependencyProperty ControlsVisibilityProperty =
            DependencyProperty.Register("ControlsVisibility", typeof(Visibility), typeof(VisualizationWindow), new PropertyMetadata(Visibility.Collapsed));

        

        public new InputBindingCollection InputBindings
        {
            get { return (InputBindingCollection)GetValue(InputBindingsProperty); }
            set { SetValue(InputBindingsProperty, value); }
        }
        public static readonly DependencyProperty InputBindingsProperty =
            DependencyProperty.Register("InputBindings", typeof(InputBindingCollection), typeof(VisualizationWindow), new PropertyMetadata(null));


        public new CommandBindingCollection CommandBindings
        {
            get { return (CommandBindingCollection)GetValue(CommandBindingsProperty); }
            set { SetValue(CommandBindingsProperty, value); }
        }
        public static readonly DependencyProperty CommandBindingsProperty =
            DependencyProperty.Register("CommandBindings", typeof(CommandBindingCollection), typeof(VisualizationWindow), new PropertyMetadata(null));

                
                
        public bool CaptureRightClick
        {
            get { return (bool)GetValue(CaptureRightClickProperty); }
            set { SetValue(CaptureRightClickProperty, value); }
        }
        public static readonly DependencyProperty CaptureRightClickProperty =
            DependencyProperty.Register("CaptureRightClick", typeof(bool), typeof(VisualizationWindow), new PropertyMetadata(false));

        /// <summary>
        /// Hold the Tracker instance used to find which widget is under the mouse pointer.
        /// </summary>
        private Tracker _tracker = null;

        /// <summary>
        /// An image of the windows contents
        /// </summary>
        public BitmapSource Thumbnail
        {
            get
            {

                Show();

                int w = 320;
                double ratio = ActualWidth / ActualHeight;
                
                return this.GetThumbnailImage(w, (int)(w / ratio));

            }
        }
        
        #endregion

        /// <summary>
        /// The default CanExecute delegate that always returns true. 
        /// Used as parameter for commands. <see cref="RelayCommand"/>
        /// </summary>
        private Func<object, object, bool> canExecute = delegate { return true; };

        #region PropertyDefinitions

        private static PropertyDefinition[] Properties = new PropertyDefinition[] {
            new PropertyDefinition() {
                Category = "Common",
                TargetProperties = new List<object>(new string[] { "Title", "FontStyle", "FontFamily", "FontWeight", "FontSize", "ShowInTaskbar" })
            },
            new PropertyDefinition() {
                Category = "Common",
                IsExpandable = true,
                TargetProperties = new List<object>(new string[] { "Background", "Foreground" })
            },
            new PropertyDefinition() {

                Category = "Layout",
                TargetProperties = new List<object>(new string[] { "RowDefinitions", "ColumnDefinitions" })
            }
        };

        #endregion
        
        public VisualizationWindow() : base()
        {
            Visibility = Visibility.Visible;
            ShowActivated = false;

            ColumnDefinitions = new ColumnDefinitionList();
            RowDefinitions    = new RowDefinitionList();
            Children          = new VisualizationControlList();
            Title             = String.Format("<{0}> [{1}]", GetType().Name, Id);

            #region Commands

            Commands.Add("Export", new RelayCommand(ExecuteCommand, canExecute, CommandType.EXPORT));
            Commands.Add("Properties", new RelayCommand(ExecuteCommand, canExecute, CommandType.PROPERTIES));
            Commands.Add("PropertiesW", new RelayCommand(ExecuteCommand, canExecute, CommandType.PROPERTIES_W));
            Commands.Add("Refresh", new RelayCommand(ExecuteCommand, canExecute, CommandType.REFRESH));
            Commands.Add("Delete", new RelayCommand(ExecuteCommand, canExecute, CommandType.DELETE));
            Commands.Add("ControlsVisibility", new RelayCommand(ExecuteCommand, canExecute, CommandType.CONTROLS));
            Commands.Add("AddChart", new RelayCommand(ExecuteCommand, canExecute, CommandType.ADD));
            Commands.Add("Save", new RelayCommand(ExecuteCommand, canExecute, CommandType.SAVE));
            Commands.Add("Undo", new RelayCommand(ExecuteCommand, delegate { return UndoExtension.Count > 0; }, CommandType.UNDO));

            #endregion

            InitializeComponent();
            
            this.DataContext = this;            

            this.Closing += VisualizationWindow_Closing;

            Dispatcher.Hooks.OperationStarted += delegate
            {
                Status = ReloadStatus.Loading;
            };

            #region Bindings

            SetBinding(ControlsVisibilityProperty, new Binding("Value") {
                Source = new BindingWrapper<Visibility>(
                    (visibility) =>
                    {

                        switch(visibility)
                        {
                            case Visibility.Visible:
                                WindowStyle = WindowStyle.SingleBorderWindow;
                                break;
                            default:
                                WindowStyle = WindowStyle.None;
                                break;
                        }

                        return visibility;
                    }),
                Mode = BindingMode.OneWayToSource
            });                       

            SetBinding(CaptureRightClickProperty, new Binding("Value")
            {
                Source = new BindingWrapper<bool>(
                    (capture) => {

                        if (_tracker == null && capture)
                        {

                                SetField(ref _tracker, _tracker == null && capture ? new Utilities.Tracker(this, Children) : null);

                                foreach (VisualizationControl element in Children)
                                {
                                    element.IsEnabled = false;
                                }

                                _tracker.MouseButtonUp += (control, e) =>
                                {

                                    if (control == null) return;
                                    if (e.ChangedButton == MouseButton.Right)
                                    {
                                        control.ContextMenu.ItemsSource = ((VisualizationControl)control).MenuItems;
                                        control.ContextMenu.IsOpen = true;
                                    }
                                };

                            }
                            else if (_tracker != null && !capture)
                            {

                                _tracker.Dispose();

                                SetField(ref _tracker, null);

                                foreach (VisualizationControl element in Children)
                                {
                                    element.IsEnabled = true;
                                }

                                foreach (VisualizationControl widget in Children)
                                {
                                    widget.HorizontalArrowsVisibility = Visibility.Collapsed;
                                }

                                 this.OnReload(this);

                            }

                            return capture;
                    }),
                Mode = BindingMode.OneWayToSource
            });

            #endregion

        }

        /// <summary>
        /// A constuctor that can be passed <see cref="VisualizationControl"/> objects as parameters and are added to the window.
        /// </summary>
        /// <param name="chart"></param>
        public VisualizationWindow(params VisualizationControl[] charts) : this()
        {
            foreach (VisualizationControl chart in charts)
            {

                if (!Children.Contains(chart))
                    Children.Add(chart);

                chart.Owner = this;
            }

        }

        
        #region Event Handlers
        
        /// <summary>
        /// Shows the left/right arrows
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Widget_MouseDblClick(object sender, MouseEventArgs e)
        {
            VisualizationControl w = ((VisualizationControl)sender);
            
            if(w.HorizontalArrowsVisibility != Visibility.Visible)
            {
                w.HorizontalArrowsVisibility = Visibility.Visible;
            }
            else
            {
                
                w.HorizontalArrowsVisibility = Visibility.Collapsed;

                this.OnReload(w);
            }
            
        }
               

        /// <summary>
        /// Called when the window close event is triggered. Cancels the close and hides instead.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void VisualizationWindow_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }

        /// <summary>
        /// Called when any of the collections (RowDefinitions, ColumnDefinitions, Children) are changed and add them to the grid.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Children_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {

            if (sender == RowDefinitions)
            {

                if (e.Action == NotifyCollectionChangedAction.Reset)

                    grid.RowDefinitions.Clear();

                if (e.OldItems != null)

                    foreach (RowDefinition d in e.OldItems)
                    {
                        grid.RowDefinitions.Remove(d.Source);
                    }


                if (e.NewItems != null)

                    foreach (RowDefinition d in e.NewItems)
                    {
                        if (!grid.RowDefinitions.Contains(d.Source))
                            grid.RowDefinitions.Add(d.Source);
                    }
            }
            else if (sender == ColumnDefinitions)
            {

                if (e.Action == NotifyCollectionChangedAction.Reset)

                    grid.ColumnDefinitions.Clear();

                if (e.OldItems != null)

                    foreach (ColumnDefinition d in e.OldItems)
                    {
                        grid.ColumnDefinitions.Remove(d.Source);
                    }

                if (e.NewItems != null)

                    foreach (ColumnDefinition d in e.NewItems)
                    {
                        if(!grid.ColumnDefinitions.Contains(d.Source))
                            grid.ColumnDefinitions.Add(d.Source);
                    }
            }
            else
            {

                if (e.OldItems != null)
                    foreach (VisualizationControl v in e.OldItems)
                    {
                        grid.Children.Remove(v);
                    }

                if (e.NewItems != null)
                    foreach (VisualizationControl widget in e.NewItems)
                    {

                        widget.Owner = this;

                        widget.RegisterUserCommand();
                        widget.RegisterUserCommand("Remove", new RelayCommand(ExecuteCommand, canExecute, CommandType.REMOVE_WIDGET, widget));

                        widget.MouseDoubleClick += Widget_MouseDblClick;

                        grid.Children.Add(widget);

                    }
            }

            foreach(VisualizationControl widget in Children)
            {
                widget.Refresh();
            }

        }
               

        #endregion

        #region Commands

        /// <summary>
        /// Holds the commands defined for the window.
        /// </summary>
        public CommandCollection Commands { get; private set;} = new CommandCollection();

        /// <summary>
        /// Defines the different command types for the window.
        /// </summary>
        private enum CommandType
        {
            /// <summary>
            /// Remove widget from this window
            /// </summary>
            REMOVE_WIDGET = -1,
            /// <summary>
            /// Show hide controls
            /// </summary>
            CONTROLS,
            /// <summary>
            /// Export to file
            /// </summary>
            EXPORT,
            /// <summary>
            /// Show properties window
            /// </summary>
            PROPERTIES,
            /// <summary>
            /// Show widget properties window
            /// </summary>
            PROPERTIES_W,
            /// <summary>
            /// Refresh and reload everything
            /// </summary>
            REFRESH,
            /// <summary>
            /// Delete this window
            /// </summary>
            DELETE,
            /// <summary>
            /// Add a widget to this window
            /// </summary>
            ADD,
            /// <summary>
            /// Save changes
            /// </summary>
            SAVE,
            /// <summary>
            /// Undo
            /// </summary>
            UNDO
        }

        /// <summary>
        /// The execute event handler that is called by <see cref="RelayCommand"/>
        /// </summary>
        /// <param name="sender">The <see cref="RelayCommand"/></param>
        /// <param name="parameter">An optional parameter passed in through XAML.</param>
        private void ExecuteCommand(object sender, object parameter)
        {
            
            PropertiesWindow prpWindow;
            RelayCommand cmd = (RelayCommand)sender;
            
            switch ((CommandType)cmd.Arguments[0].Value)
            {

                
                case CommandType.UNDO:

                    UndoExtension.Undo();
                    break;

                case CommandType.REMOVE_WIDGET:

                    Children.Remove((VisualizationControl)cmd.Arguments[1].Value);
                    break;

                case CommandType.SAVE:
                    
                    IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForDomain();

                    if (!store.DirectoryExists("windows"))
                        store.CreateDirectory("windows");

                    using (IsolatedStorageFileStream stream = store.OpenFile(String.Format(@"windows\{0}", String.Format("{0}.xaml", this.Id)), FileMode.Create))
                    {
                        MainWindow.Export(this, stream);
                    }

                    break;

                case CommandType.ADD:

                    VisualizationControl chart = (VisualizationControl)Activator.CreateInstance(((VisualizationControl.InheritorInfo)parameter).Type);

                    chart.Owner = this;

                    Children.Add(chart);

                    this.OnReload(chart);

                    foreach(object item in chart.ContextMenu.Items)
                    {
                        MenuItem m_item;

                        try
                        {
                            m_item = (MenuItem)item;
                        }
                        catch(InvalidCastException)
                        {
                            continue;
                        }

                        if ((string)m_item.Header == "Properties")
                        {
                            m_item.Command.Execute(null);
                        }
                    }

                    break;

                case CommandType.EXPORT:

                    SaveFileDialog dialog = new SaveFileDialog()
                    {
                        FileName = this.Id.ToString(),
                        DefaultExt = MainWindow.DefaultExt,
                        Filter = MainWindow.Filter,
                        RestoreDirectory = true
                    };

                    if (dialog.ShowDialog(this).Value == true)
                    {
                        MainWindow.Export(this, dialog.OpenFile());
                    }

                    break;

                case CommandType.DELETE:

                    App.Current.Dispatcher.InvokeAsync(delegate
                    {
                        ((MainWindow)App.Current.MainWindow).Remove(this);
                    });

                    break;

                case CommandType.REFRESH:
                    this.OnReload(this);
                    break;

                case CommandType.PROPERTIES:

                    PushState();

                    prpWindow = (new PropertiesWindow(this, Properties)
                    {
                        Title = String.Format("Properties [{0}]", this.Title),
                        ShowInTaskbar = false,
                        Owner = this,
                        Width = this.ActualHeight * .75,
                        Height = this.ActualWidth * .75
                    });

                    prpWindow.ShowDialog();
                    
                    break;

                case CommandType.CONTROLS:

                    ControlsVisibility = ControlsVisibility == Visibility.Hidden ? Visibility.Visible : Visibility.Hidden;

                    break;

                default:
                    break;
            }

        }

        #endregion
                
        /// <summary>
        /// Used to specify that content should not be serialized when serialzing to XAML.
        /// </summary>
        /// <returns><c>False</c> so that content will not be serialized to XAML</returns>
        public override bool ShouldSerializeContent()
        {
            return false;
        }

        /// <summary>
        /// Used to disable serialization of selected properties to XAML.
        /// </summary>
        /// <param name="dp">The Dependency Property</param>
        /// <returns><c>True</c> if the property should be serialize, <c>False</c> if not.</returns>
        protected override bool ShouldSerializeProperty(DependencyProperty dp)
        {
            //ControlsVisibility
            //    CaptureRightClick
            //    

            DependencyProperty[] properties = new DependencyProperty[]
            {
                IconProperty,
                CommandBindingsProperty,
                InputBindingsProperty,
                VisibilityProperty,
                WindowStyleProperty,
                TopmostProperty,
                NameProperty,
                CaptureRightClickProperty,
                ShowActivatedProperty,
                StatusProperty

            };

            foreach(DependencyProperty p in properties)
            {
                if (dp == p)
                    return false;
            }
            
            return base.ShouldSerializeProperty(dp);
        }
        
        /// <summary>
        /// Reloads everything in the window.
        /// </summary>
        public void Reload()
        {
            foreach(IReload chart in Children)
            {
                this.OnReload(chart);
            }
        }

        /// <summary>
        /// Overrides what should be displayed if the object is converted to text.
        /// </summary>
        /// <returns>The title</returns>
        public override string ToString()
        {
            return String.IsNullOrEmpty(Title) ? String.Format("<{0}> [{1}]", GetType().Name, Id) : Title;
        }

        /// <summary>
        /// Move all children to next results.
        /// </summary>
        /// <returns></returns>
        public bool Next()
        {
            bool r = false;

            foreach (VisualizationControl widget in Children)
                r |= widget.Next();

            return r;
        }

        /// <summary>
        /// Move all children to previous results.
        /// </summary>
        /// <returns></returns>
        public bool Previous()
        {
            bool r = false;

            foreach (VisualizationControl widget in Children)
                r |= widget.Previous();

            return r;
        }
        
        /// <summary>
        /// Pop state from the Undo stack
        /// </summary>                               
        public void PopState()
        {

            VisualizationWindow w = ((VisualizationWindow)UndoExtension.PopState(this, (dp) =>
            {

                if (dp == ContentProperty)
                    return false;

                return ShouldSerializeProperty(dp);

            }));

            w.Hide();
            w.Loaded += (sender, e) =>
             {
                 ((VisualizationWindow)sender).Close();
             };

        }
               
        /// <summary>
        /// Push state onto the Undo stack
        /// </summary>
        public void PushState()
        {
            UndoExtension.PushState(this);
        }

    }
}
