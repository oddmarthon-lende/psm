
using OxyPlot;
using OxyPlot.Series;
using PSM.Viewer.Models;
using System.Collections.Generic;
using System.Windows.Data;
using System.Windows.Media;
using Xceed.Wpf.Toolkit.PropertyGrid;

namespace PSM.Viewer.Visualizations
{

    [Visible(true)]
    [DisplayName("StairStep (OxyPlot)")]
    [Icon("../icons/chart_line.png")]
    public sealed partial class StairStep : OxyBase<StairStepSeries>
    {
        public static string DisplayName { get { return "Stair Step"; } }
        public static string Icon { get; private set; } = null;
        
        public StairStep()
        {
            InitializeComponent();

            this.SeriesAdded += StairStep_SeriesAdded;

            Properties.Add(new PropertyDefinition()
            {
                Category = "Stairstep.Stroke",
                TargetProperties = new List<object>(new string[] { "StrokeThickness", "LineStyle" })
            });

        }

        public override void Refresh()
        {
            foreach (KeyValuePair<KeyItem, StairStepSeries> s in Series)
            {
                Color color = s.Key.Color;
                s.Value.Color = OxyColor.FromArgb(color.A, color.R, color.G, color.B);
            }

            base.Refresh();
        }

        private void StairStep_SeriesAdded(MultiControl control, StairStepSeries series)
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
