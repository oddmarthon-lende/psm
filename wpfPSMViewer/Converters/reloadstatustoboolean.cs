using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace PSMViewer.Converters
{
    class ReloadStatusToBooleanConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(value is ReloadStatus)
            {
                ReloadStatus status = (ReloadStatus)value;
                return status == ReloadStatus.Loading ? true : false;
            }

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(value is bool)
            {
                return (bool)value ? ReloadStatus.Loading : ReloadStatus.Idle;
            }

            return ReloadStatus.Unknown;
        }
    }
}
