/// <copyright file="widgettype_editor.cs" company="Baker Hughes Incorporated">
/// Copyright (c) 2015 All Rights Reserved
/// </copyright>
/// <author>Odd Marthon Lende</author>
/// 
using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using Xceed.Wpf.Toolkit.PropertyGrid;
using Xceed.Wpf.Toolkit.PropertyGrid.Editors;

namespace PSM.Viewer.Editors
{

    public class WidgetTypeEditor : EnumComboBoxEditor
    {


        /// <summary>
        /// Converter used to convert back and forth between keys and values
        /// </summary>
        public class Converter : IValueConverter
        {


            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {

                if (value == null)
                    return null;

                foreach(Visualizations.VisualizationControl.InheritorInfo info in Visualizations.VisualizationControl.List)
                {

                    if (info.Type == (Type)value)
                        return info;

                }

                return null;

            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {

                Visualizations.VisualizationControl.InheritorInfo info = value as Visualizations.VisualizationControl.InheritorInfo;

                if (value == null)
                    return null;

                return info.Type;
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
        /// <returns><see cref="Store.Stores.IStore.Index"/></returns>
        protected override IEnumerable CreateItemsSource(PropertyItem propertyItem)
        {
            return Visualizations.VisualizationControl.List.Select((info) => { return info; });
        }
    }
}
