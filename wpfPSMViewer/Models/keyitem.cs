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
    
    /// <summary>
    /// The <see cref="Key"/> wrapper class.
    /// </summary>
    public class KeyItem : Key, IReload
    {

        /// <summary>
        /// <see cref="IReload.Status"/>
        /// </summary>
        public ReloadStatus Status { get; set; } = ReloadStatus.Unknown;
        
        /// <summary>
        /// <see cref="IReload.Dispatcher"/>
        /// </summary>
        public Dispatcher Dispatcher { get; private set; } = Dispatcher.CurrentDispatcher;

        /// <summary>
        /// <see cref="IReload.Cancel"/>
        /// </summary>
        public CancellationTokenSource Cancel { get; private set; } = new CancellationTokenSource();

        private KeyItem _parent = null;

        /// <summary>
        /// Gets a reference to the parent key if there is any
        /// </summary>
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

        /// <summary>
        /// Backing field for the <see cref="Children"/> property
        /// </summary>
        private ObservableCollection<KeyItem> _children = new ObservableCollection<KeyItem>();
        /// <summary>
        /// A collection of <see cref="KeyItem"/>'s that are the children of this key.
        /// </summary>
        public ObservableCollection<KeyItem> Children
        {

            get
            {
                return _children;
            }

        }
        
        private string _path = null;

        /// <summary>
        /// Gets the full path to this key.
        /// </summary>
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

        /// <summary>
        /// The default constructor
        /// </summary>
        public KeyItem() : base(null, null) { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="key">The <see cref="Key"/> to wrap</param>
        public KeyItem(Key key) : base(key.Name, key.Type) { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="key">The key name</param>
        /// <param name="type">The data type for values</param>
        public KeyItem(string key, Type type) : base(key, type) { }
                
        /// <summary>
        /// Creates a new <see cref="KeyItem"/> from a <c>string</c>
        /// </summary>
        /// <param name="Path">The path</param>
        /// <returns>The <see cref="KeyItem"/> that was created from the provided path.</returns>
        public static KeyItem CreateFromPath(string Path)
        {

            string[] s = Path.Split('.');
            string key = s.Last();

            Array.Resize(ref s, s.Length - 1);

            string p = String.Join(".", s);

            foreach (Key k in PSM.Store(Dispatcher.CurrentDispatcher).GetKeys(p))
            {
                if(k.Name == key)
                    return new KeyItem(k) { _path = Path };
            }

            return null;
        }

        /// <summary>
        /// Reloads the children
        /// </summary>
        public void Reload()
        {

            _children.Clear();

            foreach (Key k in PSM.Store(Dispatcher.CurrentDispatcher).GetKeys(Path))
            {
                _children.Add(new KeyItem(k) { _parent = this });
            }

        }

        /// <summary>
        /// <see cref="IReload.Next"/>
        /// </summary>
        /// <returns></returns>
        public bool Next()
        {
            return false;
        }

        /// <summary>
        /// <see cref="IReload.Previous"/>
        /// </summary>
        /// <returns></returns>
        public bool Previous()
        {
            return false;
        }

        /// <summary>
        /// Overrides the <see cref="object.ToString"/> method
        /// </summary>
        /// <returns>The path</returns>
        public override string ToString()
        {
            return Path;
        }
    }
}
