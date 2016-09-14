using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace PSMonitor.Stores
{

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

            try
            {

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
            catch (Exception)
            {
                return (T)System.Convert.ChangeType(value, typeof(T));
            }

        }

    }
}
