/// <copyright file="path.cs" company="Baker Hughes Incorporated">
/// Copyright (c) 2015 All Rights Reserved
/// </copyright>
/// <author>Odd Marthon Lende</author>
/// 

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace PSMonitor
{
    /// <summary>
    /// Path Data Object
    /// </summary>
    public class Path : IEnumerable<string>
    {

        /// <summary>
        /// The namespace
        /// </summary>
        public string Namespace { get; protected set; }

        /// <summary>
        /// The Key
        /// Last component of path string
        /// </summary>
        public string Key { get; protected set; }
        
        /// <summary>
        /// Number of path components
        /// </summary>
        public int Length
        {
            get
            {
                return _components.ToArray().Length;
            }
        }

        protected IEnumerable<string> _components = new List<string>();

        /// <summary>
        /// The path components enumerator
        /// </summary>
        public IEnumerable<string> Components {

            get
            {
                return _components;
            }
        }

        /// <summary>
        /// Creates a <see cref="Path"/> object from the specified string
        /// </summary>
        /// <param name="path">The path in string format</param>
        /// <returns>The path object</returns>
        public static Path Extract(string path)
        {

            string ns;
            string key;
            List<string> components = path.Trim(' ', '\t').Split('.').ToList();

            components = path.Length == 0 ? new List<string>() : components;

            switch(components.Count)
            {
                case 0:

                    return new Path() { _components = components };

                case 1:

                    return new Path { Namespace = "", Key = components[0], _components = components };
            }

            key = components.Last();
            ns = String.Join(".", components.GetRange(0, components.Count - 1));

            return new Path { Namespace = ns, Key = key, _components = components };

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

        public IEnumerator<string> GetEnumerator()
        {
            return _components.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _components.GetEnumerator();
        }
    }
}
