/// <copyright file="keyvalueconversion.cs" company="Baker Hughes Incorporated">
/// Copyright (c) 2015 All Rights Reserved
/// </copyright>
/// <author>Odd Marthon Lende</author>
/// 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSM.Stores
{

    /// <summary>
    /// The mode used for converting values
    /// </summary>
    public enum KeyValueConversionMode
    {

        None,
        Add,
        Subtract,
        Multiply,
        Divide,
        Mapping

    }

    /// <summary>
    /// Operators used for <see cref="KeyValueConversionMappingCase"/>
    /// </summary>
    public enum KeyValueConversionMappingOperator
    {

        EQUALS,
        GREATER_THAN,
        LESS_THAN,
        GREATER_THAN_OR_EQUAL,
        LESS_THAN_OR_EQUAL

    }

    /// <summary>
    /// Model used when converting from string
    /// Cases are generated when calling <see cref="KeyValueMap.Parse(string)"/>
    /// </summary>
    public class KeyValueConversionMappingCase
    {

        /// <summary>
        /// The value that gets compared to an input variable using the <see cref="Operator"/>
        /// </summary>
        public object In { get; set; }

        /// <summary>
        /// The value returned when the case matches the criteria
        /// </summary>
        public object Out { get; set; }

        /// <summary>
        /// The comparison operator used when comparing values
        /// </summary>
        public KeyValueConversionMappingOperator Operator { get; set; }

    }

    /// <summary>
    /// The map contains all cases generated from string input
    /// </summary>
    public class KeyValueMap
    {
        /// <summary>
        /// The original text
        /// </summary>
        private string _text;

        /// <summary>
        /// Holds the cases
        /// </summary>
        private KeyValueConversionMappingCase[] _cases;
        
        /// <summary>
        /// Parse a string and create a <see cref="KeyValueMap"/>
        /// </summary>
        /// <param name="text">The text to parse</param>
        /// <returns>A new map</returns>
        public static KeyValueMap Parse(string text)
        {

            string[] ms = text.Split(';').Select((s) => { return s.Trim(' ').Trim('\t'); }).ToArray();
            List<KeyValueConversionMappingCase> mappings = new List<KeyValueConversionMappingCase>();

            foreach (string mss in ms)
            {

                string[] c = mss.Split(':').Select((s) => { return s.Trim(' ').Trim('\t'); ; }).ToArray();

                if (c.Length != 2)
                    throw new Exception("Syntax Error");

                string in_ = c[0];
                string out_ = c[1];

                KeyValueConversionMappingOperator? op = null;
                
                switch (in_[0])
                {

                    case '=':

                        op = KeyValueConversionMappingOperator.EQUALS;
                        in_ = in_.Substring(1, in_.Length);

                        break;

                    case '>':
                    case '<':

                        if (in_.Length > 1)
                            switch (in_.Substring(0, 2))
                            {

                                case ">=":

                                    op = KeyValueConversionMappingOperator.GREATER_THAN_OR_EQUAL;
                                    break;

                                case "<=":

                                    op = KeyValueConversionMappingOperator.LESS_THAN_OR_EQUAL;
                                    break;

                            }

                        if (!op.HasValue)
                        {

                            switch (in_[0])
                            {

                                case '>':

                                    op = KeyValueConversionMappingOperator.GREATER_THAN;
                                    break;

                                case '<':

                                    op = KeyValueConversionMappingOperator.LESS_THAN;
                                    break;

                            }

                            if (op.HasValue)
                            {
                                in_ = in_.Substring(1, in_.Length - 1);
                            }
                            else
                                throw new Exception("Syntax Error");

                        }
                        else
                        {
                            in_ = in_.Substring(2, in_.Length);
                        }

                        break;

                    default:

                        op = KeyValueConversionMappingOperator.EQUALS;
                        break;

                }

                double i;
                double o;

                KeyValueConversionMappingCase mapping = new KeyValueConversionMappingCase();

                if (double.TryParse(in_, out i))
                {
                    mapping.In = i;
                }
                else
                {
                    mapping.In = in_;
                }

                if (double.TryParse(out_, out o))
                {
                    mapping.Out = o;
                }
                else
                {
                    mapping.Out = out_;
                }

                mapping.Operator = op.Value;

                mappings.Add(mapping);

            }

            return new KeyValueMap() { _text = text, _cases = mappings.ToArray() };
        }

        /// <summary>
        /// Match a value with this map
        /// </summary>
        /// <param name="value_in">The value to match</param>
        /// <returns>The output value if there is a match</returns>
        public object Match(object value_in)
        {

            try
            {
                if( !(value_in is double) )
                    value_in = System.Convert.ToDouble(value_in);
            }
            catch(Exception) { }

            foreach (KeyValueConversionMappingCase case_ in _cases)
            {

                if (Type.GetTypeCode(value_in.GetType()) == Type.GetTypeCode(case_.In.GetType()))
                {

                    switch (Type.GetTypeCode(case_.In.GetType()))
                    {

                        case TypeCode.Double:


                            switch (case_.Operator)
                            {

                                case KeyValueConversionMappingOperator.EQUALS:

                                    if ((double)value_in == (double)case_.In) return case_.Out;
                                    continue;

                                case KeyValueConversionMappingOperator.GREATER_THAN:

                                    if ((double)value_in > (double)case_.In) return case_.Out;
                                    continue;

                                case KeyValueConversionMappingOperator.LESS_THAN:

                                    if ((double)value_in < (double)case_.In) return case_.Out;
                                    continue;

                                case KeyValueConversionMappingOperator.GREATER_THAN_OR_EQUAL:

                                    if ((double)value_in >= (double)case_.In) return case_.Out;
                                    continue;

                                case KeyValueConversionMappingOperator.LESS_THAN_OR_EQUAL:

                                    if ((double)value_in <= (double)case_.In) return case_.Out;
                                    continue;

                                default:

                                    break;

                            }

                            break;

                        case TypeCode.String:

                            switch (case_.Operator)
                            {

                                case KeyValueConversionMappingOperator.EQUALS:

                                    if ((string)value_in == (string)case_.In) return case_.Out;
                                    continue;

                                default:

                                    break;

                            }

                            break;
                    }



                }

            }

            return value_in;

        }

        /// <summary>
        /// <see cref="object.ToString"/>
        /// </summary>
        public override string ToString()
        {
            return _text;
        }
    }

    /// <summary>
    /// Class for converting values using different modes and syntax
    /// </summary>
    public class KeyValueConversion
    {

        protected KeyValueConversionMode? _mode;
        /// <summary>
        /// The mode used when converting values
        /// </summary>
        public virtual KeyValueConversionMode Mode
        {
            get
            {
                return _mode.HasValue ? _mode.Value : KeyValueConversionMode.None;
            }

            set
            {
                _mode = value;
                Value = _value;
            }
        }

        /// <summary>
        /// Hold the object converted from string <see cref="Value"/>
        /// </summary>
        public virtual object ConvertedValue { get; protected set; } = 0D;

        /// <summary>
        /// 
        /// </summary>
        protected string _value;
        /// <summary>
        /// Value used in conjunction with <see cref="Mode"/> to convert data values to another
        /// </summary>
        public virtual string Value
        {

            get
            {
                return _value;
            }

            set
            {

                _value = value;
                ConvertedValue = 0D;

                if (_value == null || _value.Length == 0)
                    return;

                try
                {
                    ConvertedValue = double.Parse(_value);
                }
                catch(Exception)
                {

                    try
                    {
                        ConvertedValue = KeyValueMap.Parse(_value);
                    }
                    catch(Exception)
                    {
                        
                    }

                }
                

            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public KeyValueConversion() { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="other">Other object to copy properties from</param>
        public KeyValueConversion(KeyValueConversion other)
        {
            this.Mode = other.Mode;
            this.Value = other.Value;
        }

        /// <summary>
        /// Copies properties to another object
        /// </summary>
        /// <param name="other">Object to copy properties to</param>
        public void CopyTo(KeyValueConversion other)
        {
            other.Mode = this.Mode;
            other.Value = this.Value;
        }
    }
}
