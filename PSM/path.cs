using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSMonitor
{

    public class Path
    {

        public string Namespace { get; protected set; }
        public string Key { get; protected set; }        

        public static Path Extract(string path)
        {
            string[] result; string key;

            result = path.Trim(' ', '\t').Split('.');

            if (result.Length < 2)
                throw new Exception("The path given is too short.");

            key = result[result.GetUpperBound(0)];

            Array.Resize<string>(ref result, result.Length - 1);

            return new Path { Namespace = String.Join(".", result), Key = key };

        }        

        public static bool operator ==(Path p1, Path p2)
        {
            return p1.Namespace == p2.Namespace && p1.Key == p2.Key;
        }

        public static bool operator ==(string p1, Path p2)
        {
            return p1 == p2.ToString();
        }

        public static bool operator !=(string p1, Path p2)
        {
            return p1 != p2.ToString();
        }

        public static bool operator ==(Path p1, string p2)
        {
            return p1.ToString() == p2;
        }

        public static bool operator !=(Path p1, string p2)
        {
            return p1.ToString() != p2;
        }

        public static bool operator !=(Path p1, Path p2)
        {
            return p1.Namespace != p2.Namespace || p1.Key != p2.Key;
        }

        public override bool Equals(object obj)
        {
            if (obj is Path)
            {
                return this.GetHashCode() == obj.GetHashCode();
            }

            return false;
        }

        public override int GetHashCode()
        {
            return this.ToString().GetHashCode();
        }

        public override string ToString()
        {
            return String.Join(".", Namespace, Key);
        }
    }
}
