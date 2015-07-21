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
        
        private Entry entry;

        public EntryItem(Entry e)
        {
            entry = e;
        }        

        public object Value
        {
            get { return entry.value; }
        }

        public DateTime Timestamp
        {
            get { return entry.timestamp.ToLocalTime(); }
        }

        public double Age
        {
            get { return (DateTime.Now - this.Timestamp).TotalHours; }
        }

        public static explicit operator EntryItem(Entry e)
        {
            return new EntryItem(e);
        }
    }

}
