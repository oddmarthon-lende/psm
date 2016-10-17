using PSM.Viewer.Models;
/// <copyright file="reloadstatustocursor.cs" company="Baker Hughes Incorporated">
/// Copyright (c) 2015 All Rights Reserved
/// </copyright>
/// <author>Odd Marthon Lende</author>
/// <summary>A converter to convert between <see cref="PSM.ReloadStatus"/> <see cref="System.Windows.Input.Cursor."/> </summary>
/// 
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Input;

namespace PSM.Viewer.Converters
{
    class ReloadStatusToCursorConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(value is ReloadStatus)
            {
                switch((ReloadStatus)value)
                {
                    case ReloadStatus.Loading:
                        return Cursors.Wait;
                    
                }
            }

            return Cursors.Arrow;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(value is Cursor)
            {

                if (value == Cursors.Wait)
                    return ReloadStatus.Loading;
                else if (value == Cursors.Arrow)
                    return ReloadStatus.Idle;
            }

            return ReloadStatus.Unknown;
        }
    }
}
