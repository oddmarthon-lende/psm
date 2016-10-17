/// <copyright file="entryitem.cs" company="Baker Hughes Incorporated">
/// Copyright (c) 2015 All Rights Reserved
/// </copyright>
/// <author>Odd Marthon Lende</author>
/// <summary>A wrapper class around the struct <see cref="Entry"/></summary>
/// 
using System;
using PSM;

namespace PSM.Viewer.Models
{

    /// <summary>
    /// A wrapper for <see cref="Entry"/>
    /// </summary>
    public class EntryItem
    {
        /// <summary>
        /// The <see cref="Entry"/> this class wraps
        /// </summary>
        protected Entry entry;

        /// <summary>
        /// Create a new <see cref="EntryItem"/> from an existing one.
        /// </summary>
        /// <param name="item">The <see cref="EntryItem"/> to copy</param>
        public EntryItem(EntryItem item)
        {
            entry = item.entry;
        }

        /// <summary>
        /// Create a new <see cref="EntryItem"/> from an existing <see cref="Entry"/>
        /// </summary>
        /// <param name="entry">The <see cref="Entry"/> to copy</param>
        public EntryItem(Entry entry)
        {
            this.entry = entry;
        }

        /// <summary>
        /// Gets the index
        /// </summary>
        public object Index
        {
            get { return entry.Index; }
        }
        
        /// <summary>
        /// Gets the value
        /// </summary>
        public virtual object Value
        {
            get { return entry.Value; }
        }

        /// <summary>
        /// Gets the timestamp
        /// </summary>
        public DateTime Timestamp
        {
            get {
                return entry.Timestamp;
            }
        }
        
        /// <summary>
        /// Makes it possible to explicitly cast an <see cref="Entry"/> to <see cref="EntryItem"/>
        /// </summary>
        public static explicit operator EntryItem(Entry e)
        {
            return new EntryItem(e);
        }

        /// <summary>
        /// Makes it possible to explicitly cast an <see cref="EntryItem"/> to <see cref="Entry"/>
        /// </summary>
        public static explicit operator Entry(EntryItem e)
        {
            return e.entry;
        }
    }

}
