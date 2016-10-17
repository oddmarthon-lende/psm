/// <copyright file="bindingwrapper.cs" company="Baker Hughes Incorporated">
/// Copyright (c) 2015 All Rights Reserved
/// </copyright>
/// <author>Odd Marthon Lende</author>
/// <summary>Used to forward bound data properties to another object. </summary>
///

using System;

namespace PSM.Viewer.Utilities
{
    /// <summary>
    /// Can be used as a binding source so that when the value changes the provided delegate is called.
    /// </summary>
    /// <typeparam name="T">The type of the <see cref="Value"/> property.</typeparam>
    public class BindingWrapper<T>
    {

        /// <summary>
        /// The delegate to call when the value is set.
        /// </summary>
        private Func<T, T> SetValue;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="SetValue"></param>
        public BindingWrapper(Func<T, T> SetValue)
        {
            this.SetValue = SetValue;
        }

        /// <summary>
        /// The <see cref="Value"/> backing field.
        /// </summary>
        private T _value = default(T);

        /// <summary>
        /// The current value
        /// </summary>
        public T Value
        {
            get
            {
                return _value;
            }

            set
            {
                _value = SetValue(value);
            }
        }

    }

}
