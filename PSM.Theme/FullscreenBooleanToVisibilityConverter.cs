using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PSM.Theme
{
    public class FullscreenBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch((bool)value)
            {
                case true:
                    return Visibility.Collapsed;
            }

            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch((Visibility)value)
            {
                case Visibility.Visible:
                    return false;
            }

            return true;
        }
    }
}
