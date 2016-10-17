/// <copyright file="store_options_editor.cs" company="Baker Hughes Incorporated">
/// Copyright (c) 2015 All Rights Reserved
/// </copyright>
/// <author>Odd Marthon Lende</author>

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Threading;
using Xceed.Wpf.Toolkit.PropertyGrid;
using Xceed.Wpf.Toolkit.PropertyGrid.Editors;

namespace PSM.Viewer.Editors
{

    /// <summary>
    /// A class used to create a combobox editor for object that implements the <see cref="Store.Stores.IOptions"/> interface.
    /// </summary>
    public class StoreOptionEditor : ComboBoxEditor
    {

        /// <summary>
        /// Gets the <see cref="Store.Stores.Properties"/> instance from the current store.
        /// </summary>
        private static Stores.Properties Properties
        {
            get
            {
                return Store.Get(Dispatcher.CurrentDispatcher).Options.Get();
            }
        } 

        /// <summary>
        /// Converter used to convert back and forth between keys and values
        /// </summary>
        public class Converter : IValueConverter
        {
            private PropertyDescriptor _descriptor;

            public Converter(PropertyDescriptor descriptor)
            {
                this._descriptor = descriptor;
            }

            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {

                if (value == null)
                    return null;

                foreach(var p in Properties[_descriptor])
                {
                    if (p.Value.Equals(value))
                        return p;
                }

                return null;

            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {

                if (value is KeyValuePair<object, object>)
                    return ((KeyValuePair<object, object>)value).Value;

                return null;
            }

        }

        /// <summary>
        /// The propertydescriptor this object is associated with
        /// </summary>
        private PropertyDescriptor _descriptor;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="descriptor">The propertydescriptor this object is associated with.</param>
        public StoreOptionEditor(PropertyDescriptor descriptor)
        {
            this._descriptor = descriptor;
        }
        
        /// <summary>
        /// Creates the <see cref="Converter"/> instance
        /// </summary>
        /// <returns>The converter <see cref="IValueConverter"/> interface</returns>
        protected override IValueConverter CreateValueConverter()
        {
            return new Converter(_descriptor);
        }

        /// <summary>
        /// Gets the items enumerable for the <paramref name="propertyItem"/>
        /// </summary>
        /// <param name="propertyItem">The property item to get items for</param>
        /// <returns>A <see cref="Dictionary{object, object}"/> that contains the valid keys/values for the property</returns>
        protected override IEnumerable CreateItemsSource(PropertyItem propertyItem)
        {

            List<KeyValuePair<object, object>> dict;

            if (Properties.TryGetValue(propertyItem.PropertyDescriptor, out dict))
                return dict;

            return null;

        }
    }
}
