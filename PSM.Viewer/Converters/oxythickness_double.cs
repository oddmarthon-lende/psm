/// <copyright file="oxythickness_double.cs" company="Baker Hughes Incorporated">
/// Copyright (c) 2015 All Rights Reserved
/// </copyright>
/// <author>Odd Marthon Lende</author>
/// <summary>A converter to convert between <c>double</c> and <see cref="OxyThickness"/></summary>
/// 
using OxyPlot;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace PSM.Viewer.Converters
{
    public class DoubleToOxyThicknessConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            OxyThickness thickness = (OxyThickness)value;
            return (thickness.Top + thickness.Bottom + thickness.Left + thickness.Right) / 4;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double thickness = (double)value;
            return new OxyThickness(thickness);
        }
    }
}
