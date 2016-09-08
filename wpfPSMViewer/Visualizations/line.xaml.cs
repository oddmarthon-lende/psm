using OxyPlot.Series;
using System.Windows.Data;
using OxyPlot;
using System.Collections.Generic;
using Xceed.Wpf.Toolkit.PropertyGrid;
using PSMViewer.ViewModels;
using System.Collections.ObjectModel;
using PSMonitor;
using OxyPlot.Axes;
using System;
using PSMViewer.Models;
using System.Windows.Media;

namespace PSMViewer.Visualizations
{

    [Visible(true)]
    [DisplayName("Line (OxyPlot)")]
    [Icon("../icons/chart_line.png")]
    [SubCategory("OxyPlot")]
    public sealed partial class Line : OxyBase<TwoColorLineSeries>
    {
                
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

        public override void Refresh()
        {
            foreach (KeyValuePair<KeyItem, TwoColorLineSeries> s in Series)
            {
                Color color = s.Key.Color;
                s.Value.Color = OxyColor.FromArgb(color.A, color.R, color.G, color.B);
            }

            base.Refresh();
        }

        private void Line_SeriesAdded(MultiControl control, TwoColorLineSeries series)
        {
            //series.ItemsSource = null;

            //control.Entries.CollectionChanged += (sender, args) =>
            //{
            //    if(args.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            //    {
            //        foreach(EntryItem entry in args.NewItems)
            //        {
            //            series.Points.Add(new DataPoint(DateTimeAxis.ToDouble(entry.Index), control.Key.Convert<double>((Entry)entry)));
            //        }
            //    }
            //    else if(args.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset)
            //    {
            //        series.Points.Clear();
            //    }
            //};
            
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
