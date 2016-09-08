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
    public class Properties : Dictionary<PropertyDescriptor, List<KeyValuePair<object, object>>> { };
    
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
        /// Get data
        /// </summary>
        /// <param name="path">The key path</param>
        /// <param name="start">The start index</param>
        /// <param name="end">The end index</param>
        /// <param name="index">The index identifier</param>
        /// <returns>An <see cref="IEnumerable{Entry}"/> of the data</returns>
        IEnumerable<Entry> Read(string path, object start, object end, Enum index);
        
        /// <summary>
        /// Add data to the store
        /// </summary>
        /// <param name="envelope">The data <see cref="Envelope"/> that will be added. </param>
        void Write(Envelope envelope);

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
        /// Register for retrieval of realtime data.
        /// </summary>
        /// <param name="context">An object that will be used to identify the request.</param>
        /// <param name="path">The path to the key.</param>
        /// <param name="startingIndex">The starting index</param>
        /// <param name="indexIdentifier">The index used when fetching data</param>
        /// <param name="handler">The delegate that will receive the data <see cref="Envelope"/></param>
        void Register(object context, string path, object startingIndex, Enum indexIdentifier, RealTimeData handler);

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

    public enum KeyValueConversionMode
    {
        None,
        Add,
        Subtract,
        Multiply,
        Divide

    }

    public class KeyValueConversion
    {
        public KeyValueConversionMode Mode { get; set; } = KeyValueConversionMode.None;

        public double Value { get; set; }

        public KeyValueConversion()
        {

        }

        public KeyValueConversion(KeyValueConversion other)
        {
            this.Mode = other.Mode;
            this.Value = other.Value;
        }

        public void CopyTo(KeyValueConversion other)
        {
            other.Mode = this.Mode;
            other.Value = this.Value;
        }
    }

    /// <summary>
    /// A data model that describes a key
    /// </summary>
    /// 
    [Serializable]
    public class Key : ISerializable
    {
        
        /// <summary>
        /// The key name
        /// </summary>
        public string Name { get; private set; }
        /// <summary>
        /// The data type for this key's <see cref="Entry.Value"/>
        /// </summary>
        public Type Type { get; private set; }

        /// <summary>
        /// The value conversion parameters
        /// </summary>
        public KeyValueConversion Conversion { get; set; } = new KeyValueConversion();

        /// <summary>
        /// Create a new Key with the provided <paramref name="name"/>  and <paramref name="type"/>
        /// </summary>
        /// <param name="name"></param>
        /// <param name="type"></param>
        public Key(string name, Type type)
        {
            this.Name = name;
            this.Type = type;
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

        public T Convert<T>(Entry entry)
        {
            return Convert<T>(entry.Value);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Value"></param>
        /// <returns></returns>
        public T Convert<T>(object value)
        {

            double? v = null;

            if (value is Entry)
                return Convert<T>(((Entry)value).Value);
            else if (value.GetType().Equals(typeof(T)))
                return (T)value;

            switch (Type.GetTypeCode(value.GetType()))
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

                    if (System.Text.RegularExpressions.Regex.IsMatch((string)value, @"^(\d+)", System.Text.RegularExpressions.RegexOptions.Compiled))
                    {

                        double _value;

                        if (double.TryParse((string)value, out _value))
                        {
                            v = _value;
                            break;
                        }

                    }

                    v = 0D;

                    break;
            }

            try {

                v = (v.HasValue ? v.Value : System.Convert.ToDouble(value));

                switch (Conversion.Mode)
                {
                    case KeyValueConversionMode.Add:
                        v += Conversion.Value;
                        break;
                    case KeyValueConversionMode.Divide:
                        v /= Conversion.Value;
                        break;
                    case KeyValueConversionMode.Multiply:
                        v *= Conversion.Value;
                        break;
                    case KeyValueConversionMode.Subtract:
                        v -= Conversion.Value;
                        break;
                }

                return (T)System.Convert.ChangeType(v.Value, typeof(T));

            }
            catch(Exception)
            {
               return (T)System.Convert.ChangeType(value, typeof(T));
            }

        }

    }

}
