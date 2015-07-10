using System;
using System.Runtime.Serialization;
using System.Text;

namespace PSMonitor
{

    [Serializable]
    [KnownType(typeof (Entry))]
    [KnownType(typeof(Entry[]))]
    public sealed class Envelope : ISerializable
    {

        public string path;
        public Entry[] entries;
        public DateTime timestamp;

        [NonSerialized]
        public int retry = 0;

        public Envelope()
        {
            path = "";
            entries = new Entry[0];
            timestamp = DateTime.Now;
        }

        public Envelope(SerializationInfo info, StreamingContext context)
        {

            path      = info.GetString("path");
            timestamp = (DateTime)info.GetValue("timestamp", typeof(DateTime));
            entries   = (Entry[])info.GetValue("entries", typeof(Entry[]));
            
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            
            info.AddValue("path", path);
            info.AddValue("entries", entries, typeof(Entry[]));
            info.AddValue("timestamp", timestamp, typeof(DateTime));
        }

        public override string ToString()
        {

            StringBuilder str = new StringBuilder();

            str.Append(path);            

            foreach(Entry entry in entries)
            {
                str.AppendLine();
                str.Append("\t");
                str.Append(entry);
            }
            
            return str.ToString();
           
        }
    }
}
