using OxyPlot;
using PSMViewer.Visualizations;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace PSMViewer.Converters
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
