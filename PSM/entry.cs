using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace PSMonitor
{
    [Serializable]
    public struct Entry : ISerializable
    {

        public string key;
        public object value;
        public Type type;
        public DateTime timestamp;

        [NonSerialized]
        public int retry;

        public Entry(SerializationInfo info, StreamingContext context)
        {

            key = info.GetString("key");
            type = Type.GetType(info.GetString("type"));
            value = info.GetValue("value", type);
            timestamp = info.GetDateTime("timestamp").ToLocalTime();
            retry = 0;

        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("key", key);
            info.AddValue("value", value, type);
            info.AddValue("type", type.FullName);
            info.AddValue("timestamp", timestamp.ToUniversalTime().ToString("o"));
        }

        public override string ToString()
        {
            return String.Format("{0} = {1}", key, value);
        }
    }


    public class EntryJSONStream : Stream, IDisposable
    {
        private IEnumerable<Entry> entries;
        private StreamWriter writer;

        private static DataContractJsonSerializer json = new DataContractJsonSerializer(typeof(Entry));
        private IEnumerator<Entry> enumerator;

        public EntryJSONStream(IEnumerable<Entry> _entries)
        {
            
            entries = _entries;
            writer = new StreamWriter(this, System.Text.Encoding.UTF8) { AutoFlush = true };
            enumerator = entries.GetEnumerator();

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
            enumerator.Dispose();
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

                        writer.Write(START_ARRAY);
                        state = State.OPEN;

                    }
                    else if (enumerator.MoveNext())
                    {

                        EntryJSONStream.json.WriteObject(this, enumerator.Current);

                        byte[] b = this.buffer;
                        Array.Resize<byte>(ref b, b.Length + 1);

                        writer.Write(COMMA);

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

                        writer.Write(END_ARRAY);
                       
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
