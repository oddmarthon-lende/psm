/// <copyright file="reloadstatustoboolean.cs" company="Baker Hughes Incorporated">
/// Copyright (c) 2015 All Rights Reserved
/// </copyright>
/// <author>Odd Marthon Lende</author>
/// <summary>A converter to convert between <see cref="PSMViewer.ReloadStatus"/> and <see cref="System.Boolean"/> </summary>
/// 
using System;
using System.Globalization;
using System.Windows.Data;

namespace PSMViewer.Converters
{
    /// <summary>
    /// <see cref="IValueConverter"/>
    /// </summary>
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
