
using OxyPlot;
using OxyPlot.Series;
using PSM.Viewer.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

        private static PropertyDescriptor _lineStyleProperty = KeyItem.RegisterProperty(typeof(StairStep), "LineStyle", LineStyle.Automatic, new Attribute[] { new DescriptionAttribute("Linestyle for the part of the line that is above the limit"), new CategoryAttribute("StairStep.Style") });
        private static PropertyDescriptor _strokeThicknessProperty = KeyItem.RegisterProperty(typeof(StairStep), "StrokeThickness", 1.0, new Attribute[] { new DescriptionAttribute("The strokethickness"), new CategoryAttribute("StairStep.Style") });


        public StairStep()
        {
            InitializeComponent();
        }

        public override void Refresh()
        {
            foreach (KeyValuePair<KeyItem, StairStepSeries> s in Series)
            {
                Color color = (Color)KeyColorPropertyDescriptor.GetValue(s.Key);
                s.Value.Color = OxyColor.FromArgb(color.A, color.R, color.G, color.B);
                s.Value.LineStyle = (LineStyle)_lineStyleProperty.GetValue(s.Key);
                s.Value.StrokeThickness = (double)_strokeThicknessProperty.GetValue(s.Key);
                
            }

            base.Refresh();
        }

    }
}
