/// <copyright file="key.cs" company="Baker Hughes Incorporated">
/// Copyright (c) 2015 All Rights Reserved
/// </copyright>
/// <author>Odd Marthon Lende</author>
/// 
using System;
using System.Runtime.Serialization;

namespace PSM.Stores
{

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
        public string Name { get; protected set; }
        /// <summary>
        /// The data type for this key's <see cref="Entry.Value"/>
        /// </summary>
        public Type Type { get; protected set; }

        /// <summary>
        /// The value conversion parameters
        /// </summary>
        public KeyValueConversion Conversion { get; protected set; } = new KeyValueConversion();

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

            try
            {

                switch (Conversion.Mode)
                {

                    case KeyValueConversionMode.Mapping:

                        try
                        {
                            return (T)((KeyValueMap)Conversion.ConvertedValue).Match(v.HasValue ? v.Value : value);
                        }
                        catch (Exception e)
                        {
                            Logger.Error(e);
                            return default(T);
                        }
                }

                v = (v.HasValue ? v.Value : System.Convert.ToDouble(value));                

                switch (Conversion.Mode)
                {

                    case KeyValueConversionMode.Add:
                        v += (double)Conversion.ConvertedValue;
                        break;
                    case KeyValueConversionMode.Divide:
                        v /= (double)Conversion.ConvertedValue;
                        break;
                    case KeyValueConversionMode.Multiply:
                        v *= (double)Conversion.ConvertedValue;
                        break;
                    case KeyValueConversionMode.Subtract:
                        v -= (double)Conversion.ConvertedValue;
                        break;                    
                }

                return (T)System.Convert.ChangeType(v.Value, typeof(T));

            }
            catch (Exception)
            {
                return (T)System.Convert.ChangeType(value, typeof(T));
            }

        }

    }
}
