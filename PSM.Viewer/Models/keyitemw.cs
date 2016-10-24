/// <copyright file="keyitemw.cs" company="Baker Hughes Incorporated">
/// Copyright (c) 2015 All Rights Reserved
/// </copyright>
/// <author>Odd Marthon Lende</author>
/// 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Threading;
using System.Windows.Media;
using System.ComponentModel;
using PSM.Stores;
using PSM.Viewer.Utilities;

namespace PSM.Viewer.Models
{
    /// <summary>
    /// Wildcard class
    /// </summary>
    public class KeyItemW : KeyItem, INotifyPropertyChanged, IDisposable
    {
        
        /// <summary>
        /// 
        /// </summary>
        private System.Timers.Timer _timer = new System.Timers.Timer(15000);

        /// <summary>
        /// 
        /// </summary>
        private bool _auto = false;
        /// <summary>
        /// 
        /// </summary>
        public bool AutoRefresh
        {
            get
            {
                return _auto;
            }

            set
            {
                SetField(ref _auto, value);

                if (_auto)
                    _timer.Start();
                else
                    _timer.Stop();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public new string Name
        {
            get
            {
                return _parent.Name;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public override string Path
        {
            get
            {
                _path = null;
                return base.Path.TrimEnd('.');
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public override Color Color
        {
            get
            {
                return base.Color;
            }

            set
            {

                base.Color = value;

                foreach(KeyItem k in Children)
                {
                    if(!k._color.HasValue)
                    {
                        k.OnPropertyChanged("Color");
                        k.OnPropertyChanged("Brush");
                    }
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        public override string StaticPath
        {
            get
            {
                return _parent.StaticPath;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool HasWildcards { get; private set; } = false;


        /// <summary>
        /// 
        /// </summary>
        private Dictionary<string, IKeyItem> _memory = new Dictionary<string, IKeyItem>();
        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<KeyItemPath> Memory
        {
            get
            {
                List<KeyItemPath> list = new List<KeyItemPath>();

                foreach (KeyValuePair<string, IKeyItem> pair in _memory)
                {
                    KeyItemPath p = new KeyItemPath(pair.Value);
                    p.Path = pair.Key;
                    list.Add(p);
                }

                return list;

            }

            set
            {
                

                foreach (KeyItemPath p in value)
                {
                    if (_memory.ContainsKey(p.Path))
                        p.CopyTo(_memory[p.Path]);
                    else
                        _memory.Add(p.Path, p.ToKeyItem());
                }
            }
        }

        
        /// <summary>
        /// Constructor
        /// </summary>
        public KeyItemW() : base()
        {

            _timer.Elapsed += Update;
            Children.CollectionChanged += Children_CollectionChanged;
            
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Children_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {

            if (e.NewItems != null && _parent != null)
            {                

                foreach (IKeyItem k in e.NewItems)
                {

                    string path = k.Path;

                    if(_parent.Variables != null && _parent.Variables.Count() > 0)
                    {

                        Variable[] variables = _parent.Variables.ToArray();

                        PSM.Path p = PSM.Path.Extract(k.Path);
                        string[] components = p.Components.ToArray();

                        for (int i = 0; i < p.Length; i++)
                        {

                            foreach (Variable v in variables)
                            {
                                if (v.Position == i)
                                {
                                    components[i] = "*";
                                }
                            }

                        }

                        path = String.Join(".", components);
                    }
                    

                    if (_memory.ContainsKey(path))
                    {

                        _memory[path].CopyTo(k);
                        _memory[path] = k;

                    }
                    else
                    {
                        _memory.Add(path, k);
                    }

                }

            }

        }

        /// <summary>
        /// Create a new wildcard item
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static KeyItemW Create(string path, List<string> ignored = null)
        {

            KeyItemW item = new KeyItemW() { _parent = KeyItem.Create(path), _path = null  };
            
            Path p = global::PSM.Path.Extract(item.Path);

            List<IKeyItem> keys = new List<IKeyItem>();
            List<string> components = p.Components.ToList();
            
            Regex comma_seperated_expression_identification_test = new Regex(@"\{(.+?)\}", RegexOptions.Compiled);
            
            int i = 0;
            bool _w = false;

            ignored = ignored ?? new List<string>();

            if (path.IndexOf('*') == -1 && !comma_seperated_expression_identification_test.IsMatch(path) && !ignored.Contains(path))
                keys.Add(KeyItem.Create(path));
            
            foreach (string c in components.ToArray())
            {

                Match exp_matches = comma_seperated_expression_identification_test.Match(c);

                if (exp_matches != null && exp_matches.Success)
                {

                    string[] wildcard_expression_components = exp_matches.Groups[1].Value.Trim(' ', '\t').Split(',').Select((s) => { return s.Trim(' ', '\t'); }).ToArray();

                    // Check for text that starts with - and add it to ignored list
                    foreach(string c2 in wildcard_expression_components)
                    {

                        if(c2.IndexOf('-') == 0)
                        {
                            components[i] = c2.TrimStart('-');
                            ignored.Add(String.Join(".", components));
                        }

                    }

                    foreach (string c2 in wildcard_expression_components)
                    {

                        if (c2.IndexOf('-') == 0)
                            continue;

                        components[i] = c2;
                        keys.AddRange(KeyItemW.Create(String.Join(".", components), ignored).Children);

                    }

                    _w = true;

                }
                else if (c.Contains("*"))
                {

                    string ns = String.Join(".", components.GetRange(0, i));
                    Wildcard wildcard = new Wildcard(c, RegexOptions.IgnoreCase);
                                        
                    foreach (Key k in Store.Get(Dispatcher.CurrentDispatcher).Keys(ns))
                    {
                        if (!wildcard.IsMatch(k.Name))
                            continue;
                        else if (k.Type != null && i < components.Count - 1)
                            continue;
                        else if (k.Type == null && i == components.Count - 1)
                            continue;

                        components[i] = k.Name;
                        keys.AddRange(KeyItemW.Create(String.Join(".", components), ignored).Children);

                    }

                    _w = true;
                }

                i++;
            }

            item.HasWildcards = _w;

            foreach (IKeyItem k in keys)
            {
                ((KeyItem)k).W = _w ? item : null;
                item.Children.Add(k);
            }

            item._parent.PropertyChanged += item.Update;


            return item;
        }

        private void Update(object sender = null, object e = null)
        {
            this.OnReload(this, Logger.Error);
        }

        /// <summary>
        /// <see cref="IReload.Reload"/>
        /// </summary>
        public override void Reload()
        {
            
            using (KeyItemW item = KeyItemW.Create(StaticPath))
            {

                foreach (KeyItem k in Children.ToArray())
                {
                    if (!item.Children.Contains(k))
                    {
                        Children.Remove(k);
                        k.Dispose();
                    }
                    
                }

                foreach (KeyItem k in item.Children)
                {

                    if (!Children.Contains(k))
                    {
                        k.W = k.W != null ? this : null;
                        Children.Add(k);
                    }
                    else
                        k.Dispose();
                    
                }

                item.Children.Clear();
            }

        }

        /// <summary>
        /// <see cref="IDisposable.Dispose"/>
        /// </summary>
        public override void Dispose()
        {

            _parent.PropertyChanged -= Update;

            _timer.Stop();
            _timer.Dispose();
            _parent.Dispose();
            
            base.Dispose();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="other"></param>
        public override void CopyTo(IKeyItem other)
        {
            this.Conversion.CopyTo(other.Conversion);
            this.Title.CopyTo(other.Title);
            other.Color = this.Color;

        }

        /// <summary>
        /// Overrides the <see cref="object.ToString"/> method
        /// </summary>
        /// <returns>The path</returns>
        public override string ToString()
        {
            return Path;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator ==(KeyItemW a, KeyItemW b)
        {
            if (System.Object.ReferenceEquals(a, b)) return true;
            if (((object)a) == null || ((object)b) == null) return false;

            return a.StaticPath == b.StaticPath;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator !=(KeyItemW a, KeyItemW b)
        {
            return !(a == b);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            if (obj is KeyItemW)
                return ((KeyItemW)obj).StaticPath == this.StaticPath;

            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return StaticPath.GetHashCode();
        }
    }
}
