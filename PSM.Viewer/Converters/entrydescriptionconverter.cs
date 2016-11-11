using PSM.Viewer.Models;
using System;
using System.Globalization;
using System.Windows.Data;

namespace PSM.Viewer.Converters
{
    class EntryToDescriptionConverter : IValueConverter
    {
        /// <summary>
        /// 
        /// </summary>
        public string StringFormat { get; set; } = "{0} [{1}]";

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;

            Entry entry = value as Entry;
            return String.Format(StringFormat??"", entry.Value, entry.Index);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
