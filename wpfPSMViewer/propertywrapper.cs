using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace PSMViewer
{

    /// <summary>
    /// Used to wrap a <see cref="DispatcherObject"/> to read properties when a different thread is the owner of the object.
    /// </summary>
    public class DispatcherObjectPropertyWrapper
    {

        private DispatcherObject DispatcherObject;

        object this[string name]
        {
            get
            {
                return DispatcherObject.GetValue(name);
            }

            set
            {
                DispatcherObject.SetValue(name, value);
            }
        }

        public DispatcherObjectPropertyWrapper(DispatcherObject obj)
        {
            this.DispatcherObject = obj;
        }

    }

}
