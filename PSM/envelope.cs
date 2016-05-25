/// <copyright file="envelope.cs" company="Baker Hughes Incorporated">
/// Copyright (c) 2015 All Rights Reserved
/// </copyright>
/// <author>Odd Marthon Lende</author>
/// <summary>Data Container</summary>
/// 
using System;
using System.Runtime.Serialization;
using System.Text;

namespace PSMonitor
{

    /// <summary>
    /// Class that the data and metadata
    /// </summary>
    [Serializable]
    [KnownType(typeof (Entry))]
    [KnownType(typeof(Entry[]))]
    public sealed class Envelope : ISerializable
    {

        /// <summary>
        /// The namespace path containing the <see cref="Entry"/> values
        /// </summary>
        public string Path;

        /// <summary>
        /// Data
        /// </summary>
        public Entry[] Entries;

        /// <summary>
        /// The time the object was created
        /// </summary>
        public DateTime Timestamp;
        
        /// <summary>
        /// Constructor
        /// </summary>
        public Envelope()
        {
            Path = "";
            Entries = new Entry[0];
            Timestamp = DateTime.Now;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="envelope">Creates a copy from this</param>
        public Envelope(Envelope envelope)
        {
            Path = envelope.Path;
            Entries = envelope.Entries;
            Timestamp = envelope.Timestamp;
        }

        /// <summary>
        /// Deserialization constructor
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        public Envelope(SerializationInfo info, StreamingContext context)
        {

            Path      = info.GetString("path");
            Timestamp = (DateTime)info.GetValue("timestamp", typeof(DateTime));
            Entries   = (Entry[])info.GetValue("entries", typeof(Entry[]));
            
        }
        /// <summary>
        /// <see cref="ISerializable.GetObjectData(SerializationInfo, StreamingContext)"/>
        /// </summary>
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            
            info.AddValue("path", Path);
            info.AddValue("entries", Entries, typeof(Entry[]));
            info.AddValue("timestamp", Timestamp, typeof(DateTime));
        }

        /// <summary>
        /// <see cref="object.ToString"/>
        /// </summary>
        /// <returns>The <c>string</c> representation</returns>
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
