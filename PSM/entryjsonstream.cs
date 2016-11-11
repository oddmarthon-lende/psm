/// <copyright file="entryjsonstream.cs" company="Baker Hughes Incorporated">
/// Copyright (c) 2015 All Rights Reserved
/// </copyright>
/// <author>Odd Marthon Lende</author>
/// 
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;

namespace PSM
{

    /// <summary>
    /// A stream that takes an <see cref="IEnumerable{Entry}"/> as input, and outputs a JSON text stream.
    /// Copyright (C) 2015 Odd Marthon Lende
    /// </summary>
    public class EntryJSONStream : Stream, IDisposable
    {
        /// <summary>
        /// 
        /// </summary>
        private enum State
        {
            CLOSED,
            OPEN,
            OBJECT,
            DONE
        }

        /// <summary>
        /// 
        /// </summary>
        private IEnumerable<Entry> _entries;

        /// <summary>
        /// 
        /// </summary>
        private StreamWriter _writer;

        /// <summary>
        /// 
        /// </summary>
        private static DataContractJsonSerializer _json = new DataContractJsonSerializer(typeof(Entry));

        /// <summary>
        /// 
        /// </summary>
        private IEnumerator<Entry> _enumerator;

        /// <summary>
        /// 
        /// </summary>
        private long _position = 0;

        /// <summary>
        /// 
        /// </summary>
        private static char START_ARRAY = '[';

        /// <summary>
        /// 
        /// </summary>
        private static char COMMA = ',';

        /// <summary>
        /// 
        /// </summary>
        private static char END_ARRAY = ']';        

        /// <summary>
        /// 
        /// </summary>
        private State state = State.CLOSED;

        /// <summary>
        /// 
        /// </summary>
        private byte[] buffer = new byte[0];

        /// <summary>
        /// 
        /// </summary>
        private long length = 0;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_entries"></param>
        public EntryJSONStream(IEnumerable<Entry> _entries)
        {

            this._entries = _entries;
            _writer = new StreamWriter(this, System.Text.Encoding.UTF8) { AutoFlush = true };
            _enumerator = this._entries.GetEnumerator();

        }

        /// <summary>
        /// 
        /// </summary>
        public override bool CanRead
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public override bool CanSeek
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public override bool CanWrite
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public override long Length { get { return length; } }

        /// <summary>
        /// 
        /// </summary>
        public override long Position { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public override void Flush()
        {

        }        

        /// <summary>
        /// 
        /// </summary>
        public override void Close()
        {
            base.Close();
            _enumerator.Dispose();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
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
                        if (state == State.OBJECT)
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="origin"></param>
        /// <returns></returns>
        public override long Seek(long offset, SeekOrigin origin)
        {
            return Position;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public override void SetLength(long value) { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
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
