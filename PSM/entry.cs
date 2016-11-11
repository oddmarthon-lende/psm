/// <copyright file="entry.cs" company="Baker Hughes Incorporated">
/// Copyright (c) 2015 All Rights Reserved
/// </copyright>
/// <author>Odd Marthon Lende</author>
/// <summary>Data entry model object</summary>
/// 
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace PSM
{
    /// <summary>
    /// The index collection object
    /// A dictionary with some added functionality
    /// </summary>
    [Serializable]
    public class Index : Dictionary<Enum, IComparable>, IComparable<Index>, IComparable
    {

        /// <summary>
        /// String indexer
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public IComparable this[string key]
        {
            
            get
            {

                foreach(var pair in this)
                {
                    if (pair.Key.ToString() == key)
                        return pair.Value;
                }

                return null;
            }
            
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="items"></param>
        public Index(params KeyValuePair<Enum, IComparable>[] items)
        {
            foreach(var item in items)
            {
                Add(item.Key, item.Value);
            }
        }

        /// <summary>
        /// <see cref="IComparable.CompareTo(object)"/>
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public int CompareTo(object obj)
        {
            if (!(obj is Index))
                throw new ArgumentException("obj is not the same type as this instance");

            return CompareTo((Index)obj);
        }

        /// <summary>
        /// <see cref="IComparable{Index}.CompareTo(Index)"/>
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int CompareTo(Index other)
        {
            int result = 0;

            foreach(var pair in this)
            {
                if (!other.ContainsKey(pair.Key))
                    result--;
                else
                    result += other[pair.Key].CompareTo(pair.Value);
            }

            return result;

        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string[] text = new string[this.Count];
            int i = 0;

            foreach(var pair in this)
            {
                text[i++] = String.Format("{0}:{1}", pair.Key, pair.Value is DateTime ? ((DateTime)pair.Value).ToString("o"): pair.Value);
            }

            return String.Join(", ", text);
        }
    }

    /// <summary>
    /// Data Access Interface
    /// </summary>
    public interface IEntry
    {
        /// <summary>
        /// The index
        /// </summary>
        Index Index { get; }

        /// <summary>
        /// The value
        /// </summary>
        object Value { get; }
    }

    /// <summary>
    /// Class that wraps one data entry
    /// </summary>
    [Serializable]
    [KnownType(typeof(Index))]
    public class Entry : ISerializable, IEntry
    {        

        /// <summary>
        /// The data path
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// The index value for this entry
        /// </summary>
        public Index Index { get; private set; } = new Index();
        
        /// <summary>
        /// The data value for this entry
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// The value type
        /// </summary>
        public Type Type { get; set; }
                
        /// <summary>
        /// Constructor
        /// </summary>
        public Entry() { }

        /// <summary>
        /// Constructor
        /// </summary>
        public Entry(Entry other)
        {
            Key = other.Key;
            Index = other.Index;
            Value = other.Value;
            Type = other.Type;
        }

        /// <summary>
        /// Deserialization Constructor
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        public Entry(SerializationInfo info, StreamingContext context)
        {

            Index = (Index)info.GetValue("index", typeof(Index));
            Key = info.GetString("key");
            Type = Type.GetType(info.GetString("type"));
            Value = info.GetValue("value", Type);
            
        }

        /// <summary>
        /// Constructor used for serialization.
        /// <see cref="ISerializable.GetObjectData(SerializationInfo, StreamingContext)"/>
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("index", Index);
            info.AddValue("key", Key);
            info.AddValue("value", Value, Type);
            info.AddValue("type", Type.FullName);
        }

        /// <summary>
        /// Gets a string representation of this object.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return (Value??"").ToString();
        }
    }

    
}
