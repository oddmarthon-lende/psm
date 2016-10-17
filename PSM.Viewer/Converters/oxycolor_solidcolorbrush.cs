/// <copyright file="oxycolor_solidcolorbrush.cs" company="Baker Hughes Incorporated">
/// Copyright (c) 2015 All Rights Reserved
/// </copyright>
/// <author>Odd Marthon Lende</author>
/// <summary>A converter to convert between <see cref="SolidColorBrush"/> and <see cref="OxyColor"/></summary>
/// 
using OxyPlot;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace PSM.Viewer.Converters
{
    public class SolidColorBrushToOxyColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            OxyColor color = (OxyColor)value;
            return new SolidColorBrush(new Color() { R = color.R, G = color.G, B = color.B, A = color.A });
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            SolidColorBrush brush = (SolidColorBrush)value;

            if (brush == null)
                return OxyColors.Transparent;

            return OxyColor.FromArgb(brush.Color.A, brush.Color.R, brush.Color.G, brush.Color.B);
        }
    }
}
