/// <copyright file="entryitem.cs" company="Baker Hughes Incorporated">
/// Copyright (c) 2015 All Rights Reserved
/// </copyright>
/// <author>Odd Marthon Lende</author>
/// <summary>A wrapper class around the struct <see cref="Entry"/></summary>
/// 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PSMonitor;

namespace PSMViewer.Models
{

    public class EntryItem
    {
        
        protected Entry entry;

        public EntryItem(EntryItem e)
        {
            entry = e.entry;
        }

        public EntryItem(Entry e)
        {
            entry = e;
        }
        
        public virtual object Value
        {
            get { return entry.Value; }
        }

        public DateTime Timestamp
        {
            get { return entry.Timestamp.ToLocalTime(); }
        }
        
        public static explicit operator EntryItem(Entry e)
        {
            return new EntryItem(e);
        }

        public static explicit operator Entry(EntryItem e)
        {
            return e.entry;
        }
    }

}
