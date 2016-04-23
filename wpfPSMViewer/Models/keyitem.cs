﻿/// <copyright file="keyitem.cs" company="Baker Hughes Incorporated">
/// Copyright (c) 2015 All Rights Reserved
/// </copyright>
/// <author>Odd Marthon Lende</author>
/// <summary>A wrapper class around the struct <see cref="Key"/> </summary>
/// 
using System;
using System.Collections.Generic;
using System.Linq;
using PSMonitor.Stores;
using System.Collections.ObjectModel;
using PSMonitor;
using System.Windows.Threading;
using System.Threading;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Collections.Concurrent;
using System.Reflection;

namespace PSMViewer.Models
{

    /// <summary>
    /// The <see cref="Key"/> wrapper class.
    /// </summary>
    public class KeyItem : Key, IReload, INotifyPropertyChanged
    {
        
        private static ConcurrentDictionary<object, ObservableCollection<Variable>> _variables_global = new ConcurrentDictionary<object, ObservableCollection<Variable>>();

        /// <summary>
        /// Get the variables defined
        /// </summary>
        /// <param name="context">The variable context</param>
        public static ObservableCollection<Variable> GetGlobalVariables(object context = null)
        {
            context = context ?? Dispatcher.CurrentDispatcher;
            return _variables_global.ContainsKey(context) ? _variables_global[context] : null;
        }

        /// <summary>
        /// The local variables for this instance
        /// </summary>
        public IEnumerable<Variable> Variables { get; private set; }

        /// <summary>
        /// Key path variable
        /// </summary>
        public class Variable : INotifyPropertyChanged, IReload
        {

            /// <summary>
            /// The name of the variable
            /// </summary>
            public string Name { get; private set; }

            /// <summary>
            /// The parent <see cref="KeyItem"/>
            /// </summary>
            public KeyItem Parent { get; private set; }

            /// <summary>
            /// The variable position in its path
            /// </summary>
            public int Position { get; private set; }

            /// <summary>
            /// The index of the value to get from the <see cref="Keys"/> 
            /// </summary>
            private int _index = 0;

            public event PropertyChangedEventHandler PropertyChanged;

            /// <summary>
            /// Triggers the <see cref="INotifyPropertyChanged.PropertyChanged"/> event
            /// </summary>
            /// <param name="propertyName"></param>
            protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = "")
            {
                PropertyChangedEventHandler handler = PropertyChanged;
                if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
            }

            /// <summary>
            /// The selected index in the <see cref="Keys"/>
            /// </summary>
            public int SelectedIndex
            {

                get
                {
                    return _index;
                }

                set
                {

                    _index = Math.Max(Math.Min(value, Keys.Count - 1), 0);

                    OnPropertyChanged();
                    OnPropertyChanged("Value");
                    OnPropertyChanged("Path");

                    foreach(KeyValuePair<object, ObservableCollection<Variable>> p in _variables_global)
                    {
                        ObservableCollection<Variable> vars = p.Value;

                        if (vars.Contains(this))
                        {

                            foreach (Variable v in vars)
                            {

                                if(v.Position > this.Position)
                                {
                                    v.OnPropertyChanged("SelectedIndex");
                                    v.Reload();
                                }

                            }

                            break;
                        }
                    }

                }

            }

            /// <summary>
            /// The selected key
            /// </summary>
            public string Value {

                get
                {

                    int i = 0;
                    
                    foreach (Key k in Keys)
                    {
                        if (i++ == SelectedIndex)
                            return k.Name;
                    }

                    if(SelectedIndex > 0)
                    {
                        SelectedIndex = 0;
                        return Value;
                    }                        
                    
                    return null;
                }

                set
                {

                    int i = 0;

                    foreach (Key k in Keys)
                    {
                        if (k.Name == value)
                        {

                            SelectedIndex = i;
                            break;
                        }

                        i++;
                    }
                }

            }

            /// <summary>
            /// The possible values the variable can hold;
            /// </summary>
            public ObservableCollection<Key> Keys { get; private set; } = new ObservableCollection<Key>();

            public Dispatcher Dispatcher
            {
                get
                {
                    return Dispatcher.CurrentDispatcher;
                }
            }

            public CancellationTokenSource CancellationTokenSource { get; set; } = new CancellationTokenSource();

            public ReloadStatus Status { get; set; } = ReloadStatus.Idle;

            /// <summary>
            /// Constructor
            /// </summary>
            public Variable()
            {
                this.OnReload(this);
            }

            /// <summary>
            /// <see cref="object.ToString"/>
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return Name;
            }

            /// <summary>
            /// Parse and create variables from a path
            /// </summary>
            /// <param name="path">The path to parse</param>
            /// <returns>The variables found in the path</returns>
            public static IEnumerable<Variable> Parse(string path)
            {

                List<Variable> variables = new List<Variable>();
                List<string> components = new List<string>(path.Split('.'));

                Variable variable;

                foreach(Match match in Regex.Matches(path, @"\{.+?\}"))
                {
                                           
                    string parent = match.Index > 0 ? path.Substring(0, match.Index - 1) : null;
                    string name = match.Value.TrimStart('{').TrimEnd('}');

                    variable = new Variable();

                    foreach (Match m1 in Regex.Matches(match.Value, String.Format(@"(?<=:)\d+", name)))
                    {
                        if (m1.Success)
                        {
                            variable.SelectedIndex = Convert.ToInt32(m1.Value);

                            foreach(Match m2 in Regex.Matches(name, String.Format(@"(.+?)\:\d+", name)))
                            {
                                if (m2.Success)
                                {
                                    name = m2.Value.Split(':')[0];
                                }
                            }
                        }
                    }

                    variable.Name = name;
                    variable.Position = components.IndexOf(match.Value);
                    variable.Parent = parent == null ? null : KeyItem.CreateFromPath(parent);

                    variables.Add(variable);
                    variable.Reload();
                }

                return variables;
            }

            public void Reload()
            {

                Dispatcher.Invoke(delegate
                {

                    Keys.Clear();

                    foreach ( Key k in PSM.Store(Dispatcher).Keys(Parent == null ? "" : Parent.Path) )
                    {
                        Keys.Add(k);
                    }

                    OnPropertyChanged("Value");
                    OnPropertyChanged("Path");

                });

            }

            public bool Next()
            {
                return false;
            }

            public bool Previous()
            {
                return false;
            }
        }

        private Enum _indexIdentifier;

        /// <summary>
        /// The index identifier
        /// </summary>
        public Enum IndexIdentifier {

            get
            {
                return _indexIdentifier ?? (_indexIdentifier = PSMonitor.PSM.Store(Dispatcher).Default);
            }

            set
            {
                _indexIdentifier = value;
            }

        }

        private ReloadStatus _status = ReloadStatus.Idle;
        /// <summary>
        /// <see cref="IReload.Status"/>
        /// </summary>
        public ReloadStatus Status
        {

            get
            {
                return _status;
            }

            set
            {
                SetField(ref _status, value);
            }

        }
        
        /// <summary>
        /// <see cref="IReload.Dispatcher"/>
        /// </summary>
        public Dispatcher Dispatcher { get; private set; } = Dispatcher.CurrentDispatcher;

        /// <summary>
        /// <see cref="IReload.CancellationTokenSource"/>
        /// </summary>
        public CancellationTokenSource CancellationTokenSource { get; set; } = new CancellationTokenSource();

        private KeyItem _parent = null;

        /// <summary>
        /// Gets a reference to the parent key if there is any
        /// </summary>
        public KeyItem Parent {

            get {

                if(_parent == null && _path != null)
                {

                    string[] s = _path.Split('.');
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

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Triggers the <see cref="INotifyPropertyChanged.PropertyChanged"/> event
        /// </summary>
        /// <param name="propertyName"></param>
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Conveniance method
        /// </summary>
        protected bool SetField<TField>(ref TField field, TField value, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<TField>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);

            return true;
        }

        /// <summary>
        /// <see cref="Key.Name"/>
        /// </summary>
        public new string Name
        {
            get
            {
                
                if(Variables != null)
                    foreach(Variable v in Variables)
                    {
                        if ( (Parent != null ? Parent.Path.Split('.').Length : 0) == v.Position)
                        {
                            return v.Value;
                        }
                    }

                return base.Name;
            }
        }

        /// <summary>
        /// Gets the full path to this key.
        /// </summary>
        public string Path
        {
            get
            {
                
                if (_path == null || (Variables != null && Variables.ToArray().Length > 0) )
                {

                    KeyItem parent = this.Parent;
                    List<string> names = new List<string>();

                    names.Add(this.Name);

                    while (parent != null && parent.Name != null)
                    {

                        if(parent.Path != null)
                        {
                            names.Add(parent.Path);
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
        /// Gets the static path, without any variable values
        /// </summary>
        public string StaticPath
        {

            get
            {

                string[] components = this.Path.Split('.');
                int i = 0;

                if (Variables == null)
                    return Path;

                foreach(string component in components)
                {

                    foreach(Variable v in Variables)
                    {
                        if(i == v.Position)
                        {
                            components[i] = '{' + v.Name + '}';
                        }
                    }

                    i++;

                }
                
                return String.Join(".", components);

            }

        }

        private object _context = null;
        /// <summary>
        /// The variable context
        /// </summary>
        public object Context {

            get {
                return _context ?? Dispatcher.CurrentDispatcher;
            }

            set {
                _context = value;
            }

        }

        /// <summary>
        /// Constructor
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
        /// <param name="path">The path</param>
        /// <param name="ctx">The variable context</param>
        /// <returns>The <see cref="KeyItem"/> that was created from the provided path.</returns>
        public static KeyItem CreateFromPath(string path, object ctx = null)
        {

            if (path == null || path.Length == 0)
                return null;

            ctx = ctx ?? Dispatcher.CurrentDispatcher;
                        
            List<Variable> vars = Variable.Parse(path).ToList();
            
            string[] s = path.Split('.');
            string key = s.Last();            

            Array.Resize(ref s, s.Length - 1);

            string p = String.Join(".", s);

            if(vars.Count > 0)
            {

                ObservableCollection<Variable> variables_global_ctx;
                
                if ( !_variables_global.ContainsKey(ctx) )
                {

                    variables_global_ctx = new ObservableCollection<Variable>();
                    while(!_variables_global.TryAdd(ctx, variables_global_ctx));
                }
                else
                    variables_global_ctx = _variables_global[ctx];

                int i = 0;

                foreach (Variable v_local in vars.ToArray())
                {                   

                    foreach ( Variable v_global in variables_global_ctx )
                    {

                        if (v_global.Name == v_local.Name && v_global.Position == v_local.Position)
                        {
                            vars[i] = v_global;
                        }
                        else if (v_global.Name == v_local.Name)
                        {
                            throw new Exception("Duplicate variable name");
                        }

                    }

                    if(vars[i++] == v_local)
                        variables_global_ctx.Add(v_local);

                }               
            }

            KeyItem item = new KeyItem(key, null);

            item.Context = ctx;
            item.Variables = vars;
            item._parent = KeyItem.CreateFromPath(p);
            item._path = p.Length > 0 ? p : null;

            if(item._parent != null)
            {
              
                foreach (Key k in PSM.Store(ctx).Keys(item._parent.Path))
                {
                    item = new KeyItem(key, k.Type)
                    {
                        Context = ctx,
                        Variables = vars.Count > 0 ? vars : null,
                        _parent = item._parent
                    };

                    break;
                }

            }

            return item;
        }

        /// <summary>
        /// Reloads the children
        /// </summary>
        public void Reload()
        {

            _children.Clear();

            foreach (Key k in PSM.Store(Dispatcher.CurrentDispatcher).Keys(Path))
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
