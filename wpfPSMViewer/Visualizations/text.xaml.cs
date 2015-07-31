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
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

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
                if(Parent.Highlighting.HighlightThresholdExceeded)
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

        public DateTime LastResetTime { get; set; } = new DateTime(1999, 7, 2, 17, 42, 0);
        
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

        private SolidColorBrush _lineColor = new SolidColorBrush(Color.FromRgb(0, 0, 0));
        public SolidColorBrush LineColor {
            get { return _lineColor; }
            set { SetField(ref _lineColor, value); }
        }

        private double _keyFontSize;
        public double KeyFontSize {
            get { return _keyFontSize; }
            set { SetField(ref _keyFontSize, value); }
        }

        private double _pathFontSize;
        public double PathFontSize {
            get { return _pathFontSize; }
            set { SetField(ref _pathFontSize, value); }
        }

        private Visibility _showKey = Visibility.Hidden;
        public Visibility ShowKey {
            get { return _showKey; }
            set { SetField(ref _showKey, value); }
        }

        private Visibility _showPath = Visibility.Hidden;
        public Visibility ShowPath {
            get { return _showPath; }
            set { SetField(ref _showPath, value); }
        }

        public DataTemplate ItemTemplate
        {
            get
            {
                return (DataTemplate)uniformGrid.FindResource(String.Format("{0}Template", Orientation));
            }
        }

        public Text()
        {

            InitializeComponent();

            PropertyDefinitions.Add(new PropertyDefinition()
            {
                Category = "Text",
                TargetProperties = new List<object>(new string[] { "KeyFontSize", "PathFontSize", "LineColor" })
            });

            PropertyDefinitions.Add(new PropertyDefinition()
            {
                Category = "Text.Layout",
                TargetProperties = new List<object>(new string[] { "Orientation", "ShowKey", "ShowPath" })
            });

            PropertyDefinitions.Add(new PropertyDefinition()
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
            RESET = 100
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
    }
}
