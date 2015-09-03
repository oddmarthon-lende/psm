/// <copyright file="text.xaml.cs" company="Baker Hughes Incorporated">
/// Copyright (c) 2015 All Rights Reserved
/// </copyright>
/// <author>Odd Marthon Lende</author>
/// <summary>Code behind for the Text Widget</summary>

using PSMViewer.Models;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Xceed.Wpf.Toolkit.PropertyGrid;
using System.Windows.Media;
using System.Globalization;
using PSMViewer.Visualizations;
using System.Windows.Controls.Primitives;
using System.Linq;
using System.Collections.ObjectModel;

namespace PSMViewer.Converters
{

    public class TextHighlightColorConverter : DependencyObject, IValueConverter
    {

        public Text Parent
        {
            get { return (Text)GetValue(ParentProperty); }
            set { SetValue(ParentProperty, value); }
        }

        public static readonly DependencyProperty ParentProperty =
            DependencyProperty.Register("Parent", typeof(Text), typeof(TextHighlightColorConverter), new PropertyMetadata(null));

        public TextHighlightColorConverter(Text parent)
        {
            this.Parent = parent;
        }

        public TextHighlightColorConverter()
        {

        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            EntryItem data = (EntryItem)value;
            double? d_value = null;
            
            try
            {
                if(Parent.Highlighting.HighlightThresholdExceeded && !(data.Value is string))
                    d_value = System.Convert.ToDouble(data.Value);
            }
            catch(Exception) { }

            int t = Parent.Highlighting.HighlightThresholdExceeded && Parent.LastResetTime < data.Timestamp && (d_value != null && ((d_value > Parent.Threshold.High) || (d_value < Parent.Threshold.Low))) ? 2 : 0;
            int n = Parent.Highlighting.HighlightNew && Parent.LastResetTime < data.Timestamp ? 1 : 0;

            switch(t | n)
            {
                case 0:
                    return Parent.Foreground;
                case 1:
                    return Parent.Highlighting.New;
                case 2:
                    return Parent.Highlighting.Threshold;
                case 3:
                    return new LinearGradientBrush(Parent.Highlighting.New.Color, Parent.Highlighting.Threshold.Color, 90.0);
            }

            return null;
            
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

namespace PSMViewer.Visualizations
{

    public class AggregatedEntryItem : EntryItem
    {
        private KeyItem _key;
        public KeyItem Key
        {
            get
            {
                return _key;
            }
        }

        public AggregatedEntryItem(KeyItem key, EntryItem entry) : base(entry)
        {
            _key = key;
        }
    }
    
    public class Threshold
    {
        public double Low { get; set; }
        public double High { get; set; }
    }

    public class Highlight
    {
        public bool HighlightThresholdExceeded { get; set; }
        public bool HighlightNew { get; set; }

        public SolidColorBrush New { get; set; }
        public SolidColorBrush Threshold { get; set; }
    }
    
    public sealed partial class Text : VisualizationControl
    {        
        
        public static string DisplayName { get { return typeof(Text).Name; } }
        public static string Icon { get; private set; } = "../icons/text_linespacing.png";

        

        private Orientation _orientation = Orientation.Horizontal;
        public Orientation Orientation {
            get {
                return _orientation;
            }
            set {
                SetField(ref _orientation, value);
                Refresh();
            }
        }

        public DateTime LastResetTime { get; set; } = new DateTime(1970, 1, 1);
        
        public Highlight Highlighting { get; set; } = new Highlight() { New = new SolidColorBrush(Color.FromArgb(255, 255, 255, 0)), Threshold = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0)), HighlightNew = false, HighlightThresholdExceeded = false };
        public Threshold Threshold { get; set; } = new Threshold() { Low = 0, High = 1 };

        public double MaxColumnWidth
        {
            get
            {
                return Orientation == Orientation.Horizontal ? (ActualWidth / Controls.Count) : double.PositiveInfinity;
            }
        }

        public double MaxColumnHeight
        {
            get
            {
                return Orientation == Orientation.Vertical ? (ActualHeight / Controls.Count) : double.PositiveInfinity;
            }
        }

       

        public SolidColorBrush LineColor
        {
            get { return (SolidColorBrush)GetValue(LineColorProperty); }
            set { SetValue(LineColorProperty, value); }
        }
        public static readonly DependencyProperty LineColorProperty =
            DependencyProperty.Register("LineColor", typeof(SolidColorBrush), typeof(Text), new PropertyMetadata(null));




        public double KeyFontSize
        {
            get { return (double)GetValue(KeyFontSizeProperty); }
            set { SetValue(KeyFontSizeProperty, value); }
        }public static readonly DependencyProperty KeyFontSizeProperty =
            DependencyProperty.Register("KeyFontSize", typeof(double), typeof(Text), new PropertyMetadata(double.NaN));




        public double PathFontSize
        {
            get { return (double)GetValue(PathFontSizeProperty); }
            set { SetValue(PathFontSizeProperty, value); }
        }
        public static readonly DependencyProperty PathFontSizeProperty =
            DependencyProperty.Register("PathFontSize", typeof(double), typeof(Text), new PropertyMetadata(double.NaN));


        /// <summary>
        /// Visibility of the key name at the top of each column
        /// </summary>
        public Visibility ShowKey
        {
            get { return (Visibility)GetValue(ShowKeyProperty); }
            set { SetValue(ShowKeyProperty, value); }
        }
        public static readonly DependencyProperty ShowKeyProperty =
            DependencyProperty.Register("ShowKey", typeof(Visibility), typeof(Text), new PropertyMetadata(Visibility.Collapsed));


        /// <summary>
        /// Visibility of the parents path at the side of each column
        /// </summary>
        public Visibility ShowPath
        {
            get { return (Visibility)GetValue(ShowPathProperty); }
            set { SetValue(ShowPathProperty, value); }
        }
        public static readonly DependencyProperty ShowPathProperty =
            DependencyProperty.Register("ShowPath", typeof(Visibility), typeof(Text), new PropertyMetadata(Visibility.Collapsed));


        private AggregatedEntryItem _item = null;
        /// <summary>
        /// It is used to display the parents path and key name when the data is aggregated into 1 column. The value will be set to what is under the mouse.
        /// </summary>
        public AggregatedEntryItem CurrentItem
        {
            get
            {
                return _item;
            }

            private set
            {
                SetField(ref _item, value);
            }

        }


        private bool _aggregate = false;
        /// <summary>
        /// Display the data in 1 column sorted by time
        /// </summary>
        public bool Aggregate
        {
            get
            {
                return _aggregate;
            }

            set
            {
                _aggregate = value;

                switch(_aggregate)
                {
                    case true:
                        Content = (Grid)uniformGrid.FindResource("AggregateContent");
                        break;
                    default:
                        Content = uniformGrid;
                        break;
                }
            }
        }

        /// <summary>
        /// The item template for the items control based on <see cref="Orientation"/>
        /// </summary>
        public DataTemplate ItemTemplate
        {
            get
            {
                return (DataTemplate)uniformGrid.FindResource(String.Format("{0}Template", Orientation));
            }
        }

        /// <summary>
        /// Overrides the VisualizationControl.Add method add connects to the DataChanged event, so that the display is updated when the data changes in aggregate mode.
        /// </summary>
        /// <param name="key">The key to add</param>
        /// <param name="collection">An alternate collection for the data</param>
        /// <returns>The added control</returns>
        public override MultiControl Add(KeyItem key, ObservableCollection<EntryItem> collection = null)
        {

            MultiControl controls = base.Add(key, collection);

            if(controls != null && Aggregate )
                controls.DataChanged += Controls_DataChanged;
            
            return controls;
        }

        /// <summary>
        /// So that the display is updated when the data changes in aggregate mode.
        /// </summary>
        /// <param name="sender"></param>
        private void Controls_DataChanged(object sender)
        {
            OnPropertyChanged("Entries");
        }

        /// <summary>
        /// An aggregated enumerable of entries from all keys
        /// </summary>
        public IEnumerable<EntryItem> Entries
        {

            get
            {
                return Enumerable.SelectMany<MultiControl, AggregatedEntryItem>(Controls, m =>
                {

                    return m.Entries.Select(e =>
                    {
                        return new AggregatedEntryItem(m.Key, e);
                    });

                }).OrderByDescending<AggregatedEntryItem, DateTime>((entry) =>
                {
                    return entry.Timestamp;
                });

            }

        }
        

        /// <summary>
        /// The default constructor
        /// </summary>
        public Text()
        {
            
            InitializeComponent();
                        
            Properties.Add(new PropertyDefinition()
            {
                Category = "Text",
                TargetProperties = new List<object>(new string[] { "Aggregate", "KeyFontSize", "PathFontSize", "LineColor" })
            });

            Properties.Add(new PropertyDefinition()
            {
                Category = "Text.Layout",
                TargetProperties = new List<object>(new string[] { "Orientation", "ShowKey", "ShowPath" })
            });

            Properties.Add(new PropertyDefinition()
            { 

                IsExpandable = true,
                Category = "Text.Highlighting",
                TargetProperties = new List<object>(new string[] { "Highlighting", "Threshold" })
            });

            CommandsSource.Add("Reset", new RelayCommand());

            RegisterUserCommand();
            RegisterUserCommand("Reset Highlighting", CommandsSource["Reset"]);

            Controls.CollectionChanged += delegate {
                OnPropertyChanged("MaxColumnWidth");
                OnPropertyChanged("MaxColumnHeight");
            };

            LineColor = new SolidColorBrush(Color.FromRgb(0, 0, 0));

        }


        public override void Refresh()
        {
            OnPropertyChanged("Highlighting");
            OnPropertyChanged("Threshold");
            OnPropertyChanged("ItemTemplate");
            OnPropertyChanged("MaxColumnWidth");
            OnPropertyChanged("MaxColumnHeight");
            
            base.Refresh();
        }

        private new enum CommandType
        {
            RESET
        }

        protected override void ExecuteCommand(object sender, object parameter)
        {
            RelayCommand cmd = (RelayCommand)sender;
            
            switch ((CommandType)cmd.Arguments[0].Value)
            {
                case CommandType.RESET:
                    LastResetTime = DateTime.Now;
                    break;
            }

            base.ExecuteCommand(sender, parameter);
        }

        protected override bool ShouldSerializeProperty(DependencyProperty dp)
        {
            switch(dp.Name)
            {
                case "FontStyle":
                case "FontFamily":
                case "FontWeight":
                case "FontSize":
                    return true;
            }

            return base.ShouldSerializeProperty(dp);
        }

        private void scroller_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {

            ScrollViewer viewer = (ScrollViewer)sender;

            Button down = (Button)LogicalTreeHelper.FindLogicalNode(viewer.Parent, "down");
            Button up = (Button)LogicalTreeHelper.FindLogicalNode(viewer.Parent, "up");

            down.Visibility = viewer.ExtentHeight > viewer.ActualHeight && (viewer.ExtentHeight - viewer.VerticalOffset) > viewer.ActualHeight ? Visibility.Visible : Visibility.Collapsed;
            up.Visibility = viewer.VerticalOffset > 0 ? Visibility.Visible : Visibility.Collapsed;

        }

        private void TextBlock_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            TextBlock text = (TextBlock)sender;
            CurrentItem = (AggregatedEntryItem)text.DataContext;

        }
    }
}
