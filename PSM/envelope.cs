/// <copyright file="envelope.cs" company="Baker Hughes Incorporated">
/// Copyright (c) 2015 All Rights Reserved
/// </copyright>
/// <author>Odd Marthon Lende</author>
/// <summary>Data Container</summary>
/// 
using System;
using System.Runtime.Serialization;

namespace PSM
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
        public string Path { get; set; }

        /// <summary>
        /// Data
        /// </summary>
        public Entry[] Entries { get; set; }        
        
        /// <summary>
        /// Constructor
        /// </summary>
        public Envelope()
        {
            Path = "";
            Entries = new Entry[0];
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="envelope">Creates a copy from this</param>
        public Envelope(Envelope envelope)
        {
            Path = envelope.Path;
            Entries = envelope.Entries;
        }

        /// <summary>
        /// Deserialization constructor
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        public Envelope(SerializationInfo info, StreamingContext context)
        {

            Path      = info.GetString("path");
            Entries   = (Entry[])info.GetValue("entries", typeof(Entry[]));
            
        }
        /// <summary>
        /// <see cref="ISerializable.GetObjectData(SerializationInfo, StreamingContext)"/>
        /// </summary>
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            
            info.AddValue("path", Path);
            info.AddValue("entries", Entries, typeof(Entry[]));
        }

        /// <summary>
        /// <see cref="object.ToString"/>
        /// </summary>
        /// <returns>The <c>string</c> representation</returns>
        public override string ToString()
        {
            return Path;           
        }
    }
}
