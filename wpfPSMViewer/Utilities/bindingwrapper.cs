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
