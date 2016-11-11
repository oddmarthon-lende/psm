using PSM.Viewer.Commands;
using PSM.Viewer.Models;
using PSM.Viewer.Visualizations;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Xceed.Wpf.Toolkit.PropertyGrid;
using System.Linq;

namespace PSM.Viewer.Converters
{
    public class TextHighlightColorConverter : DependencyObject, IValueConverter
    {

        public TableBase Parent
        {
            get { return (TableBase)GetValue(ParentProperty); }
            set { SetValue(ParentProperty, value); }
        }

        public static readonly DependencyProperty ParentProperty =
            DependencyProperty.Register("Parent", typeof(TableBase), typeof(TextHighlightColorConverter), new PropertyMetadata(null));

        public TextHighlightColorConverter(TableBase parent)
        {
            this.Parent = parent;
        }

        public TextHighlightColorConverter()
        {

        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return Parent.Foreground;

            Entry data = (Entry)value;
            double? d_value = null;

            try
            {
                if (Parent.Highlighting.HighlightThresholdExceeded && !(data.Value is string))
                    d_value = System.Convert.ToDouble(data.Value);
            }
            catch (Exception) { }

            DateTime ts = (DateTime)data.Index.Where((pair) => pair.Value.GetType() == typeof(DateTime)).First().Value;
            int t = Parent.Highlighting.HighlightThresholdExceeded && Parent.LastResetTime < ts && (d_value != null && ((d_value > Parent.Threshold.High) || (d_value < Parent.Threshold.Low))) ? 2 : 0;
            int n = Parent.Highlighting.HighlightNew && Parent.LastResetTime < ts ? 1 : 0;

            switch (t | n)
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

namespace PSM.Viewer.Visualizations
{

    public class Threshold
    {
        public double Low { get; set; } = 0;
        public double High { get; set; } = 1;
    }

    public class Highlight
    {
        public bool HighlightThresholdExceeded { get; set; } = false;

        public bool HighlightNew { get; set; } = false;

        public SolidColorBrush New { get; set; } = new SolidColorBrush(Color.FromArgb(255, 255, 255, 0));

        public SolidColorBrush Threshold { get; set; } = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
        
    }

    [Visible(false)]
    public class TableBase : VisualizationControl
    {        

        public DateTime LastResetTime { get; set; } = new DateTime(1970, 1, 1);

        public Highlight Highlighting
        {
            get { return (Highlight)GetValue(HighlightingProperty); }
            set { SetValue(HighlightingProperty, value); }
        }
        public static readonly DependencyProperty HighlightingProperty =
            DependencyProperty.Register("Highlighting", typeof(Highlight), typeof(TableBase), new PropertyMetadata(null));
        
        public Threshold Threshold
        {
            get { return (Threshold)GetValue(ThresholdProperty); }
            set { SetValue(ThresholdProperty, value); }
        }

        public static readonly DependencyProperty ThresholdProperty =
            DependencyProperty.Register("Threshold", typeof(Threshold), typeof(TableBase), new PropertyMetadata(null));
               

        public TableBase()
        {

            Properties.Add(new PropertyDefinition()
            {

                IsExpandable = true,
                Category = "Table.Highlighting",
                TargetProperties = new List<object>(new string[] { "Highlighting", "Threshold" })
            });
            
            RegisterUserCommand();
            RegisterUserCommand("Reset Highlighting", new RelayCommand((a,b) => ResetHighlighting(), canExecute));

            Threshold = new Threshold();
            Highlighting = new Highlight();

        }

        public virtual void ResetHighlighting()
        {
            LastResetTime = DateTime.Now;
            this.UpdateBindingTargets(TextBlock.ForegroundProperty);
        }


    }
}
