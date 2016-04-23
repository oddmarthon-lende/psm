using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSMonitor.Stores
{
    public class Dummy : Store
    {

        public enum IndexType
        {
            Dummy
        }

        public override Enum Default
        {
            get
            {
                return IndexType.Dummy;
            }
        }

        public override Type Index
        {
            get
            {
                return typeof(IndexType);
            }
        }

        public override long Delete(string path)
        {
            throw new NotImplementedException();
        }

        public override long Delete(string path, object start, object end, Enum index)
        {
            throw new NotImplementedException();
        }

        private Random _randy = new Random();

        public override Entry Get(string path)
        {

            return new Entry()
            {
                Index = 0,
                Timestamp = DateTime.Now,
                Key = Path.Extract(path).Key,
                Type = typeof(double),
                Value = _randy.NextDouble()
            };

        }

        public override IEnumerable<Entry> Get(string path, object start, object end, Enum index)
        {

            long s    = (long)start;
            long e    = (long)end;
            string k = Path.Extract(path).Key;

            List<Entry> result = new List<Entry>();

            for(int i = 0; i <= (e - s); i++)
            {
                result.Add(new Entry()
                {
                    Index = s + i,
                    Timestamp = DateTime.Now.Subtract(new TimeSpan(0, i, 0, 0)),
                    Key = k,
                    Type = typeof(double),
                    Value = _randy.NextDouble()
                });
            }

            return result;

        }

        public override Key[] Keys(string ns)
        {
            List<Key> keys = new List<Key>();
            Path path = Path.Extract(ns);
           
            if(path.Length <= 1)
                for (int i = 0; i < 10; i++)
                {
                    keys.Add(new Key( (i * (path.Length + 1)).ToString(), path.Length > 0 ? typeof(double) : null));
                }

            return keys.ToArray();
        }

        public override void Put(Envelope envelope)
        {
            throw new NotImplementedException();
        }
    }
}
