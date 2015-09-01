using OxyPlot.Series;
using System.Windows.Data;
using OxyPlot;
using System.Collections.Generic;
using Xceed.Wpf.Toolkit.PropertyGrid;

namespace PSMViewer.Visualizations
{

    public sealed partial class Line : OxyBase<TwoColorLineSeries>
    {

        public static string DisplayName { get { return typeof(Line).Name; } }
        public static string Icon { get; private set; } = "../icons/chart_line.png";
                
        public Line()
        {
            
            InitializeComponent();        
                
            this.SeriesAdded += Line_SeriesAdded;

            Properties.Add(new PropertyDefinition()
            {
                Category = "Line.Stroke",
                TargetProperties = new List<object>(new string[] { "StrokeThickness", "LineStyle" })
            });
        }       

        private void Line_SeriesAdded(MultiControl control, TwoColorLineSeries series)
        {

            SetBinding(LineStyleProperty, new Binding("Value")
            {
                Source = new Utilities.BindingWrapper<LineStyle>(

                    style => {
                        series.LineStyle = style;                 
                        return style;
                    }
            ),
                Mode = BindingMode.OneWayToSource
            });

            SetBinding(StrokeThicknessProperty, new Binding("Value")
            {
                Source = new Utilities.BindingWrapper<double>(

                    thickness => {
                        series.StrokeThickness = thickness;
                        return thickness;
                    }
            ),
                Mode = BindingMode.OneWayToSource
            });

        }

    }
}
