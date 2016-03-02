/// <copyright file="entry.cs" company="Baker Hughes Incorporated">
/// Copyright (c) 2015 All Rights Reserved
/// </copyright>
/// <author>Odd Marthon Lende</author>
/// <summary>Data entry model object</summary>
/// 
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace PSMonitor
{
    
    /// <summary>
    /// Class that wraps one data entry
    /// </summary>
    [Serializable]
    public struct Entry : ISerializable
    {

        /// <summary>
        /// The data path
        /// </summary>
        public string Key;

        /// <summary>
        /// The index value for this entry
        /// </summary>
        public object Index;

        /// <summary>
        /// The data value for this entry
        /// </summary>
        public object Value;

        /// <summary>
        /// The value type
        /// </summary>
        public Type Type;

        /// <summary>
        /// The timestamp for this entry.
        /// </summary>
        public DateTime Timestamp;

        [NonSerialized]
        public int Retry;

        /// <summary>
        /// Deserialization Constructor
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        public Entry(SerializationInfo info, StreamingContext context)
        {
            Index = info.GetValue("index", typeof(object));
            Key = info.GetString("key");
            Type = Type.GetType(info.GetString("type"));
            Value = info.GetValue("value", Type);
            Timestamp = info.GetDateTime("timestamp").ToLocalTime();            
            Retry = 0;

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
            info.AddValue("timestamp", Timestamp.ToUniversalTime().ToString("o"));
        }

        /// <summary>
        /// Gets a string representation of this object.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return String.Format("{0} = {1}", Key, Value);
        }
    }

    /// <summary>
    /// A stream that takes an <see cref="IEnumerable{Entry}"/> as input, and outputs a JSON text stream.
    /// Copyright (C) 2015 Odd Marthon Lende
    /// </summary>
    public class EntryJSONStream : Stream, IDisposable
    {
        private IEnumerable<Entry> _entries;
        private StreamWriter _writer;

        private static DataContractJsonSerializer _json = new DataContractJsonSerializer(typeof(Entry));
        private IEnumerator<Entry> _enumerator;

        public EntryJSONStream(IEnumerable<Entry> _entries)
        {
            
            this._entries = _entries;
            _writer = new StreamWriter(this, System.Text.Encoding.UTF8) { AutoFlush = true };
            _enumerator = this._entries.GetEnumerator();

        }

        public override bool CanRead
        {
            get
            {
                return true;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return false;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return true;
            }
        }
        
        public override long Length { get { return length; } }

        public override long Position { get; set; }
        
        public override void Flush()
        {
        }

        private long _position = 0;

        private static char START_ARRAY = '[';
        private static char COMMA = ',';
        private static char END_ARRAY = ']';

        private enum State
        {
            CLOSED,
            OPEN,
            OBJECT,
            DONE
        }

        private State state = State.CLOSED;

        public override void Close()
        {
            base.Close();
            _enumerator.Dispose();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            
            int i = offset;

            for (; state != State.DONE && i < offset + count; i++)
            {

                long p = Position;

                if (Position >= length)
                {

                    if (state == State.CLOSED)
                    {

                        _writer.Write(START_ARRAY);
                        state = State.OPEN;

                    }
                    else if (_enumerator.MoveNext())
                    {

                        EntryJSONStream._json.WriteObject(this, _enumerator.Current);

                        byte[] b = this.buffer;
                        Array.Resize<byte>(ref b, b.Length + 1);

                        _writer.Write(COMMA);

                        b[b.Length - 1] = this.buffer[0];
                        this.buffer = b;

                        state = State.OBJECT;

                    }
                    else
                    {
                        if(state == State.OBJECT)
                        {
                            Position--; i--;
                        }                        

                        _writer.Write(END_ARRAY);
                       
                        state = State.DONE;

                    }

                }

                Position = p;

                buffer[i] = this.buffer[_position++];

                Position++;

            }
            
            return i - offset;

        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return Position;
        }

        public override void SetLength(long value)
        {
        }

        private byte[] buffer = new byte[0];
        private long length = 0;

        public override void Write(byte[] buffer, int offset, int count)
        {

            if (Position + count > length)
                length += count;

            _position = 0;

            this.buffer = new byte[count];

            for (int i = 0; i < count && i < length; i++)
            {

                this.buffer[i] = buffer[offset + i];
                Position++;

            }
        }
    }
}
