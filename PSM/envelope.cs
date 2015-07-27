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

        public string Path;
        public Entry[] Entries;
        public DateTime Timestamp;

        [NonSerialized]
        public int Retry = 0;

        public Envelope()
        {
            Path = "";
            Entries = new Entry[0];
            Timestamp = DateTime.Now;
        }

        public Envelope(SerializationInfo info, StreamingContext context)
        {

            Path      = info.GetString("path");
            Timestamp = (DateTime)info.GetValue("timestamp", typeof(DateTime));
            Entries   = (Entry[])info.GetValue("entries", typeof(Entry[]));
            
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            
            info.AddValue("path", Path);
            info.AddValue("entries", Entries, typeof(Entry[]));
            info.AddValue("timestamp", Timestamp, typeof(DateTime));
        }

        public override string ToString()
        {

            StringBuilder str = new StringBuilder();

            str.Append(Path);            

            foreach(Entry entry in Entries)
            {
                str.AppendLine();
                str.Append("\t");
                str.Append(entry);
            }
            
            return str.ToString();
           
        }
    }
}
