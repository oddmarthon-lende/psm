/// <copyright file="keyitemvalueconversion.cs" company="Baker Hughes Incorporated">
/// Copyright (c) 2015 All Rights Reserved
/// </copyright>
/// <author>Odd Marthon Lende</author>
/// 
using PSM.Stores;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PSM.Viewer.Models
{
    /// <summary>
    /// Wrapper class for <see cref="KeyValueConversion"/> that implements <see cref="INotifyPropertyChanged"/> and inherits from <see cref="IKeyItem.W"/>
    /// </summary>
    public class KeyItemValueConversion : KeyValueConversion, INotifyPropertyChanged
    {

        /// <summary>
        /// The <see cref="IKeyItem"/> this object belongs to
        /// </summary>
        public IKeyItem Key { get; private set; }

        /// <summary>
        /// <see cref="INotifyPropertyChanged.PropertyChanged"/>
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Triggers the <see cref="INotifyPropertyChanged.PropertyChanged"/> event
        /// </summary>
        /// <param name="propertyName"></param>
        internal virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Conveniance method
        /// </summary>
        protected bool SetField<TField>(ref TField field, TField value, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<TField>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        public override KeyValueConversionMode Mode
        {
            get
            {
                return _mode.HasValue ? _mode.Value : Key.W != null ? Key.W.Conversion.Mode : base.Mode;
            }

            set
            {
                base.Mode = value;

                if (Key.W != null && Key.W.Conversion._mode == _mode)
                {
                    _mode = null;
                }

                foreach (KeyItem key in Key.Children)
                {
                    if (!key.Conversion._mode.HasValue)
                    {
                        key.Conversion.OnPropertyChanged("Mode");
                    }
                }

                OnPropertyChanged("Mode");
            }
        }
        
        /// <summary>
        /// 
        /// </summary>
        protected object _convertedValue = 0D;

        /// <summary>
        /// 
        /// </summary>
        public override object ConvertedValue
        {
            get
            {
                return _convertedValue.Equals(0D) ? Key.W != null ? Key.W.Conversion.ConvertedValue : base.ConvertedValue : _convertedValue;
            }

            protected set
            {

                _convertedValue = value;

                if (Key.W != null && Key.W.Conversion._convertedValue == _convertedValue)
                {
                    _convertedValue = 0D;
                }

                foreach (KeyItem key in Key.Children)
                {
                    if (key.Conversion._convertedValue.Equals(0D))
                    {
                        key.Conversion.OnPropertyChanged("ConvertedValue");
                    }
                }

                OnPropertyChanged("ConvertedValue");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public override string Value
        {
            get
            {
                return _value == null ? Key.W != null ? Key.W.Conversion.Value : base.Value : base.Value;
            }

            set
            {

                base.Value = value;

                if (Key.W != null && _value == Key.W.Conversion._value)
                {
                    _value = null;
                }

                foreach (KeyItem key in Key.Children)
                {
                    if (key.Conversion._value == null)
                    {
                        key.Conversion.OnPropertyChanged("Value");
                    }
                }

                OnPropertyChanged("Value");
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="key">The key this belongs to</param>
        public KeyItemValueConversion(IKeyItem key)
        {
            Key = key;
        }

        
    }
}
