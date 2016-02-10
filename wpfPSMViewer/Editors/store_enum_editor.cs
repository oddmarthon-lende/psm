/// <copyright file="store_enum_editor.cs" company="Baker Hughes Incorporated">
/// Copyright (c) 2015 All Rights Reserved
/// </copyright>
/// <author>Odd Marthon Lende</author>

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Threading;
using Xceed.Wpf.Toolkit.PropertyGrid;
using Xceed.Wpf.Toolkit.PropertyGrid.Editors;

namespace PSMViewer.Editors
{
    public class StoreEnumEditor : EnumComboBoxEditor
    {

        
        /// <summary>
        /// Converter used to convert back and forth between keys and values
        /// </summary>
        public class Converter : IValueConverter
        {
            
            
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                
                return value.ToString();

            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {                
                return Enum.Parse(PSMonitor.PSM.Store(Dispatcher.CurrentDispatcher).Index, (string)value);
            }

        }
        

        /// <summary>
        /// Creates the <see cref="Converter"/> instance
        /// </summary>
        /// <returns>The converter <see cref="IValueConverter"/> interface</returns>
        protected override IValueConverter CreateValueConverter()
        {
            return new Converter();
        }

        /// <summary>
        /// Gets the items enumerable for the <paramref name="propertyItem"/>
        /// </summary>
        /// <param name="propertyItem">The property item to get items for</param>
        /// <returns><see cref="PSMonitor.Stores.IStore.Index"/></returns>
        protected override IEnumerable CreateItemsSource(PropertyItem propertyItem)
        {
            return PSMonitor.PSM.Store(Dispatcher.CurrentDispatcher).Index.GetEnumNames();
        }
    }
}
