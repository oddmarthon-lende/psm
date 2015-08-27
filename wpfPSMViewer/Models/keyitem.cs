/// <copyright file="keyitem.cs" company="Baker Hughes Incorporated">
/// Copyright (c) 2015 All Rights Reserved
/// </copyright>
/// <author>Odd Marthon Lende</author>
/// <summary>A wrapper class around the struct <see cref="Key"/> </summary>
/// 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PSMonitor.Stores;
using System.Collections.ObjectModel;
using PSMonitor;
using PSMViewer.ViewModels;
using System.Windows.Threading;
using System.Threading;

namespace PSMViewer.Models
{
    
    public class KeyItem : Key, IReload
    {

        private Dispatcher _dispatcher = Dispatcher.CurrentDispatcher;
        public Dispatcher Dispatcher
        {
            get
            {
                return _dispatcher;
            }
        }

        private CancellationTokenSource _c = new CancellationTokenSource();
        public CancellationTokenSource Cancel
        {
            get
            {
                return _c;
            }
        }

        private KeyItem _parent = null;
        public KeyItem Parent {

            get {

                if(_parent == null && _path != null)
                {

                    string[] s = Path.Split('.');
                    string key = s.Last();

                    Array.Resize(ref s, s.Length - 1);

                    string p = String.Join(".", s);

                    _parent = KeyItem.CreateFromPath(p);

                }

                return _parent;
            }

        }

        private ObservableCollection<KeyItem> _children = new ObservableCollection<KeyItem>();
        public ObservableCollection<KeyItem> Children
        {

            get
            {
                return _children;
            }

        }
        
        private string _path = null;
        public string Path
        {
            get
            {

                if (_path == null)
                {

                    KeyItem parent = this.Parent;
                    List<string> names = new List<string>();

                    names.Add(this.Name);

                    while (parent != null && parent.Name != null)
                    {
                        if(parent._path != null)
                        {
                            names.Add(parent._path);
                            break;
                        }

                        names.Add(parent.Name);
                        parent = parent.Parent;
                    }

                    names.Reverse();

                    _path = string.Join<string>(".", names);

                }

                return _path ?? this.Name;
            }
        }

        public KeyItem() : base(null, null) { }
        public KeyItem(Key key) : base(key.Name, key.Type) { }
        public KeyItem(string key, Type type) : base(key, type) { }
                
        public static KeyItem CreateFromPath(string Path)
        {

            string[] s = Path.Split('.');
            string key = s.Last();

            Array.Resize(ref s, s.Length - 1);

            string p = String.Join(".", s);

            foreach (Key k in PSM.Store.GetKeys(p))
            {
                if(k.Name == key)
                    return new KeyItem(k) { _path = Path };
            }

            return null;
        }

        public void Reload()
        {

            _children.Clear();

            foreach (Key k in PSM.Store.GetKeys(Path))
            {
                _children.Add(new KeyItem(k) { _parent = this });
            }

        }

        public bool Next()
        {
            return false;
        }

        public bool Previous()
        {
            return false;
        }

        public override string ToString()
        {
            return Path;
        }
    }
}
