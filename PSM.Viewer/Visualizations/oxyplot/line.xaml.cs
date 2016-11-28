using OxyPlot.Series;
using OxyPlot;
using System.Collections.Generic;
using PSM.Viewer.Models;
using System.Windows.Media;
using System;
using System.ComponentModel;

namespace PSM.Viewer.Visualizations
{

    [Visible(true)]
    [DisplayName("Line (OxyPlot)")]
    [Icon("../icons/chart_line.png")]
    [SubCategory("OxyPlot")]
    public sealed partial class Line : OxyBase<TwoColorLineSeries>
    {

        private static PropertyDescriptor KeyColor2Property = KeyItem.RegisterProperty(typeof(Line), "Color2", OxyColors.Automatic, new Attribute[] { new DescriptionAttribute("Color for the part of the line that is below the limit"), new CategoryAttribute("Line.Style") });
        private static PropertyDescriptor KeyLineStyleProperty = KeyItem.RegisterProperty(typeof(Line), "LineStyle", LineStyle.Automatic, new Attribute[] { new DescriptionAttribute("Linestyle for the part of the line that is above the limit"), new CategoryAttribute("Line.Style") });
        private static PropertyDescriptor KeyLineStyle2Property = KeyItem.RegisterProperty(typeof(Line), "LineStyle2", LineStyle.Automatic, new Attribute[] { new DescriptionAttribute("Linestyle for the part of the line that is below the limit"), new CategoryAttribute("Line.Style") });
        private static PropertyDescriptor KeyStrokeThicknessProperty = KeyItem.RegisterProperty(typeof(Line), "StrokeThickness", 1.0, new Attribute[] { new DescriptionAttribute("The strokethickness"), new CategoryAttribute("Line.Style") });
        private static PropertyDescriptor KeySmoothProperty = KeyItem.RegisterProperty(typeof(Line), "Smooth", true, new Attribute[] { new DescriptionAttribute("Line Smoothing"), new CategoryAttribute("Line.Style") });


        public Line()
        {
            InitializeComponent();
        }

        public override void Refresh()
        {
            foreach (KeyValuePair<KeyItem, TwoColorLineSeries> s in Series)
            {

                Color color = (Color)KeyColorPropertyDescriptor.GetValue(s.Key);

                s.Value.Color = OxyColor.FromArgb(color.A, color.R, color.G, color.B);
                s.Value.LineStyle = (LineStyle)KeyLineStyleProperty.GetValue(s.Key);
                s.Value.LineStyle2 = (LineStyle)KeyLineStyle2Property.GetValue(s.Key);
                s.Value.Color2 = (OxyColor)KeyColor2Property.GetValue(s.Key);
                s.Value.StrokeThickness = (double)KeyStrokeThicknessProperty.GetValue(s.Key);
                s.Value.Smooth = (bool)KeySmoothProperty.GetValue(s.Key);
            }

            base.Refresh();
        }

        

    }
}
