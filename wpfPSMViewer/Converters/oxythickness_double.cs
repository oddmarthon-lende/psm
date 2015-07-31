using OxyPlot;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace PSMViewer.Converters
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
