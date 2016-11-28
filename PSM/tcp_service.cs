using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using PSM.Stores;

namespace PSM.Wcf
{
    /// <summary>
    /// 
    /// </summary>
    public class TCP : Stores.Store
    {
        /// <summary>
        /// 
        /// </summary>
        private static IStore _store = PSM.Store.Get();

        /// <summary>
        /// 
        /// </summary>
        public override Enum Default
        {
            get
            {
                return _store.Default;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public override Type Index
        {
            get
            {
                return _store.Index;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public TCP()
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        public override void Delete(string path)
        {
            _store.Delete(path);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ns"></param>
        /// <returns></returns>
        public override IEnumerable<Key> Keys(string ns)
        {
            return _store.Keys(ns);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public override IEnumerable<Entry> Read(string path, IComparable start, IComparable end, Enum index)
        {
            return _store.Read(path, start, end, index);
        }
    }
}
