/// <copyright file="bindingwrapper.cs" company="Baker Hughes Incorporated">
/// Copyright (c) 2015 All Rights Reserved
/// </copyright>
/// <author>Odd Marthon Lende</author>
/// <summary>Used to forward bound data properties to another object. </summary>
///

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSMViewer.Utilities
{
    public class BindingWrapper<T>
    {

        private Func<T, T> SetValue;

        public BindingWrapper(Func<T, T> SetValue)
        {
            this.SetValue = SetValue;
        }

        private T _value;
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
