/// <copyright file="istore.cs" company="Baker Hughes Incorporated">
/// Copyright (c) 2015 All Rights Reserved
/// </copyright>
/// <author>Odd Marthon Lende</author>
/// <summary>Store interface class declarations\specifications</summary>
/// 
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace PSMonitor.Stores
{

    /// <summary>
    /// A class that holds the <see cref="PropertyDescriptor"/>'s for the properties in a type that implements <see cref="IOptions"/> interface and a dictionary that can be filled with valid values for a property.
    /// </summary>
    public class Properties : Dictionary<PropertyDescriptor, Dictionary<object, object>> { };
    
    /// <summary>
    /// Interface for accessing options and get valid choice values for each option.
    /// </summary>
    public interface IOptions
    {
        
        /// <summary>
        /// Get the properties as a <see cref="Properties"/> object.
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        Properties Get();

        /// <summary>
        /// Gets the property value
        /// </summary>
        /// <typeparam name="T">The property value type</typeparam>
        /// <param name="PropertyName">The property name</param>
        /// <returns>The value</returns>
        T Get<T>(string PropertyName);

    }
        
    /// <summary>
    /// Defines the interface that is used when accessing the data store.
    /// <typeparam name="T">The type </typeparam>
    /// </summary>
    public interface IStore : IDisposable
    {

        /// <summary>
        /// The default index field
        /// </summary>
        Enum Default { get; }
        
        /// <summary>
        /// The enum type with index identifiers
        /// </summary>
        Type Index { get; }

        /// <summary>
        /// Gets an object that contains properties that can be used to configure the store and be exposed to the user.
        /// </summary>
        IOptions Options { get; }

        /// <summary>
        /// Get the current\last <see cref="Entry"/>
        /// </summary>
        /// <param name="path">The key path</param>
        /// <returns>The last data <see cref="Entry"/> that was written to the provided <paramref name="path"/></returns>
        Entry Get(string path);

        /// <summary>
        /// Get data
        /// </summary>
        /// <param name="path">The key path</param>
        /// <param name="start">The start index</param>
        /// <param name="end">The end index</param>
        /// <param name="index">The index identifier</param>
        /// <returns>An <see cref="IEnumerable{Entry}"/> of the data</returns>
        IEnumerable<Entry> Get(string path, object start, object end, Enum index);

        /// <summary>
        /// Add data to the store
        /// </summary>
        /// <param name="envelope">The data <see cref="Envelope"/> that will be added. </param>
        void Put(Envelope envelope);

        /// <summary>
        /// Delete the key and all data.
        /// </summary>
        /// <param name="path">The key path</param>
        /// <returns>The number of deleted entries</returns>
        long Delete(string path);

        /// <summary>
        /// Delete data within the specified <paramref name="start"/> and <paramref name="end"/> indexes.
        /// </summary>
        /// <param name="path">The key path</param>
        /// <param name="start">The start index</param>
        /// <param name="end">The end index</param>
        /// <param name="index">The index identifier</param>
        /// <returns>The number of deleted entries</returns>
        long Delete(string path, object start, object end, Enum index);

        /// <summary>
        /// Get a list of keys that is contained within the provided namespace
        /// </summary>
        /// <param name="ns">The namespace</param>
        /// <returns>An array that contains the <see cref="Key"/>'s</returns>
        Key[] Keys(string ns);

        /// <summary>
        /// Sets the units for a key
        /// </summary>
        /// <param name="path">The path to the key</param>
        /// <param name="units">The units</param>
        void Units(string path, Units units);

        /// <summary>
        /// Gets the units for a key
        /// </summary>
        /// <param name="path">The path to the key</param>
        /// <returns>The units</returns>
        Units Units(string path);

        /// <summary>
        /// Sets metadata for a path
        /// </summary>
        /// <param name="path">The path</param>
        /// <param name="key">The key</param>
        /// <param name="value">The value</param>
        void Meta(string path, string key, object value);

        /// <summary>
        /// Gets the metadata for a path
        /// </summary>
        /// <param name="path">The path</param>
        /// <returns>A dictionary with the metadata for the specified <paramref name="path"/></returns>
        Dictionary<string, object> Meta(string path);

        /// <summary>
        /// Register for retrieval of realtime data.
        /// </summary>
        /// <param name="context">An object that will be used to identify the request.</param>
        /// <param name="path">The path to the key.</param>
        /// <param name="startingIndex">The starting index</param>
        /// <param name="handler">The delegate that will receive the data <see cref="Envelope"/></param>
        void Register(object context, string path, object startingIndex, RealTimeData handler);

        /// <summary>
        /// Unregister the context and stop the data transfer for all keys that was <see cref="Register(object, string, object, RealTimeData)"/>d with this context.
        /// </summary>
        /// <param name="context">The context to unregister.</param>
        void Unregister(object context);

        /// <summary>
        /// Unregister the key for the provided context and stop the data transfer.
        /// </summary>
        /// <param name="context">The context that was used when the key was <see cref="Register(object, string, object, RealTimeData)"/></param>d.
        /// <param name="path">The key path</param>
        void Unregister(object context, string path);

    }

    
    /// <summary>
    /// A data model that describes a key
    /// </summary>
    /// 
    [Serializable]
    public class Key : ISerializable
    {

        public Units Units { get; private set; }

        /// <summary>
        /// The key name
        /// </summary>
        public string Name { get; private set; }
        /// <summary>
        /// The data type for this key's <see cref="Entry.Value"/>
        /// </summary>
        public Type Type { get; private set; }

        /// <summary>
        /// Create a new Key with the provided <paramref name="Name"/>  and <paramref name="Type"/>
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="Type"></param>
        public Key(string Name, Type Type)
        {
            this.Name = Name;
            this.Type = Type;

            this.Units = new Units("normal"); /// < Temporary
        }

        /// <summary>
        /// ISerializable constructor
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        public Key(SerializationInfo info, StreamingContext context)
        {
            Name = info.GetString("name");
            Type = Type.GetType(info.GetString("type"), false);
        }

        /// <summary>
        /// <see cref="ISerializable.GetObjectData(SerializationInfo, StreamingContext)"/>
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("name", Name);
            info.AddValue("type", Type != null ? Type.FullName : "");
        }
        
        /// <summary>
        /// Gets a string represention of this object.
        /// </summary>
        /// <returns>The <see cref="Name"/></returns>
        public override string ToString()
        {
            return Name;
        }
    }
        

    /// <summary>
    /// Class that describes a Unit and can convert values from,to.
    /// </summary>
    [Serializable]
    public class Units
    {

        public string DisplayName { get; private set; }

        public double Factor { get; private set; }
                

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name"></param>
        /// <param name="factor">A conversion factor that scales the value up or down. Defaults to 1.</param>
        public Units(string name, double factor = 1D)
        {
            this.DisplayName = name;
            this.Factor = factor;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Value"></param>
        /// <returns></returns>
        public T Convert<T>(Entry entry)
        {

            double? v = null;

            switch (Type.GetTypeCode(entry.Value.GetType()))
            {
                
                case TypeCode.Byte:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.SByte:
                case TypeCode.Single:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Char:

                    break;

                case TypeCode.String:

                    if (System.Text.RegularExpressions.Regex.IsMatch((string)entry.Value, @"^(\d+)", System.Text.RegularExpressions.RegexOptions.Compiled))
                    {

                        double value;

                        if (double.TryParse((string)entry.Value, out value))
                        {
                            v = value;
                            break;
                        }

                    }

                    v = 0D;

                    break;
            }

            return (T)(object)(v.HasValue ? v.Value : System.Convert.ToDouble(entry.Value) * Factor);

        }

    }

}
