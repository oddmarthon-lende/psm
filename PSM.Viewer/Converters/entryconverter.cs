using PSM.Viewer.Models;
using System;
using System.Globalization;
using System.Windows.Data;

namespace PSM.Viewer.Converters
{
    class EntryToStringConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            KeyItem key = null;
            object value = null;

            foreach(object v in values)
            {
                if (v is KeyItem)
                    key = (KeyItem)v;
                else
                    value = v;
            }

            if (key != null && value != null)
            {
                return key.Convert<string>(value is EntryItem ? ((EntryItem)value).Value : value);
            }
            else if (value != null)
                return (value is EntryItem ? ((EntryItem)value).Value : value);

            return null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
