/// <copyright file="keyitem.cs" company="Baker Hughes Incorporated">
/// Copyright (c) 2015 All Rights Reserved
/// </copyright>
/// <author>Odd Marthon Lende</author>
/// <summary>A wrapper class around the struct <see cref="Key"/> </summary>
/// 
using System;
using System.Collections.Generic;
using System.Linq;
using PSM.Stores;
using System.Collections.ObjectModel;
using System.Windows.Threading;
using System.Threading;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.Concurrent;
using System.Windows.Media;

namespace PSM.Viewer.Models
{



    /// <summary>
    /// The <see cref="Key"/> wrapper class.
    /// </summary>
    public partial class KeyItem : Key, IReload, INotifyPropertyChanged, IKeyItem, IDisposable
    {

                
        /// <summary>
        /// 
        /// </summary>
        private static ConcurrentDictionary<object, ObservableCollection<Variable>> _variables_global = new ConcurrentDictionary<object, ObservableCollection<Variable>>();

        /// <summary>
        /// 
        /// </summary>
        private static ConcurrentDictionary<string, Key[]> _subKeyCache = new ConcurrentDictionary<string, Key[]>();

        private static ConcurrentDictionary<string, Key[]> Cache
        {
            get
            {
                if(_clearSubkeyCacheTimer == null)
                {
                    _clearSubkeyCacheTimer = new System.Timers.Timer(10000) { AutoReset = false };
                    _clearSubkeyCacheTimer.Elapsed += ClearCache;
                }

                _clearSubkeyCacheTimer.Stop();
                _clearSubkeyCacheTimer.Start();

                return _subKeyCache;
            }
        }

        private static System.Timers.Timer _clearSubkeyCacheTimer;

        /// <summary>
        /// 
        /// </summary>
        private static void ClearCache(object sender = null, object args = null)
        {
            _subKeyCache.Clear();
        }

        /// <summary>
        /// 
        /// </summary>
        public KeyItemW W { get; set; } = null;

        /// <summary>
        /// 
        /// </summary>
        protected static Random _random = new Random((int)DateTime.Now.Ticks);

        /// <summary>
        /// 
        /// </summary>
        protected KeyItemValueConversion _conversion;
        
        /// <summary>
        /// 
        /// </summary>
        public new KeyItemValueConversion Conversion
        {
            get
            {
                return _conversion;
            }

            private set
            {
                _conversion = value;
                base.Conversion = (KeyValueConversion)_conversion;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        internal Color? _color;

        /// <summary>
        /// 
        /// </summary>
        public virtual Color Color
        {
            get
            {

                if (!_color.HasValue)
                {
                    if (W != null)
                        return W.Color;

                    _color = Color.FromArgb(255, (byte)(_random.NextDouble() * 255D), (byte)(_random.NextDouble() * 255D), (byte)(_random.NextDouble() * 255D));
                }

                return _color.Value;
            }

            set
            {

                _color = value;

                if (W != null && W._color == _color)
                {
                    _color = null;
                }

                OnPropertyChanged("Color");
                OnPropertyChanged("Brush");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public SolidColorBrush Brush
        {
            get
            {
                return new SolidColorBrush(Color);
            }
        }

        private bool _typeConfirmed = false;

        private Type _type = null;

        /// <summary>
        /// 
        /// </summary>
        public new Type Type
        {
            get
            {

                if (base.Type == null && _type == null && !_typeConfirmed)
                {
                    string parent = _parent != null ? _parent.Path : "";

                    Key[] keys = Cache.ContainsKey(parent) ? Cache[parent] : Store.Get(Dispatcher).Keys(parent);

                    foreach (Key k in keys)
                    {
                        if (Name == k.Name)
                        {
                            _type = k.Type;
                            break;
                        }
                    }

                    _typeConfirmed = true;
                }

                return _type ?? base.Type ?? (Variables != null && Variables.Count() > 0 ? typeof(object) : null);
            }

            set
            {
                _type = value;
            }
        }

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
        /// The title shows the current path component at the specified position
        /// </summary>
        public KeyItemTitle Title { get; private set; }

        /// <summary>
        /// The local variables for this instance
        /// </summary>
        public IEnumerable<Variable> Variables { get; private set; }        

        private Enum _indexIdentifier;

        /// <summary>
        /// The index identifier
        /// </summary>
        public Enum IndexIdentifier {

            get
            {
                return _indexIdentifier ?? (_indexIdentifier = PSM.Store.Get(Dispatcher).Default);
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

        protected KeyItem _parent = null;

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

                    _parent = KeyItem.Create(p);

                }

                return _parent;
            }

        }

        /// <summary>
        /// Backing field for the <see cref="Children"/> property
        /// </summary>
        private ObservableCollection<IKeyItem> _children = new ObservableCollection<IKeyItem>();
        /// <summary>
        /// A collection of <see cref="KeyItem"/>'s that are the children of this key.
        /// </summary>
        public ObservableCollection<IKeyItem> Children
        {

            get
            {
                return _children;
            }

        }
              
        /// <summary>
        /// 
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Triggers the <see cref="INotifyPropertyChanged.PropertyChanged"/> event
        /// </summary>
        /// <param name="propertyName"></param>
        internal virtual void OnPropertyChanged(string propertyName)
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
                        if ( Parent == v.Parent )
                        {
                            return v.Value ?? "";
                        }
                    }

                return base.Name;
            }
        }

        protected string _path = null;
        /// <summary>
        /// Gets the full path to this key.
        /// </summary>
        public virtual string Path
        {
            get
            {
                
                if (_path == null || (Variables != null && Variables.ToArray().Length > 0) )
                {

                    KeyItem parent = this.Parent;
                    List<string> names = new List<string>();

                   names.Add(this.Name??"");

                    while (parent != null && parent.Name != null)
                    {

                        if(parent.Path != null)
                        {
                            names.Add(parent.Path);
                            break;
                        }

                        names.Add(parent.Name??"");
                        parent = parent.Parent;
                    }

                    names.Reverse();

                    _path = string.Join<string>(".", names);

                }

                return _path ?? this.Name ?? "";
            }
        }

        /// <summary>
        /// Gets the static path, without any variable values
        /// </summary>
        public virtual string StaticPath
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
                            components[i] = '$' + v.Name;
                        }
                    }

                    i++;

                }
                
                return String.Join(".", components);

            }

        }

        protected object _context = null;
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
        /// 
        /// </summary>
        private void _init()
        {
            Title = new KeyItemTitle(this);
            Conversion = new KeyItemValueConversion(this);
        }
        
        /// <summary>
        /// Constructor
        /// </summary>
        public KeyItem() : base(null, null) { _init(); }
               
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="key">The <see cref="Key"/> to wrap</param>
        public KeyItem(Key key) : base(key.Name, key.Type) { _init(); }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="key">The key name</param>
        /// <param name="type">The data type for values</param>
        public KeyItem(string key, Type type) : base(key, type) { _init(); }

        /// <summary>
        /// Creates a new <see cref="KeyItem"/> from a <c>string</c>
        /// </summary>
        /// <param name="path">The path</param>
        /// <param name="ctx">The variable context</param>
        /// <returns>The <see cref="KeyItem"/> that was created from the provided path.</returns>
        public static KeyItem Create(string path, object ctx = null, uint depth = 0)
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
                            if (v_global.Parent != v_local.Parent)
                            {
                                throw new KeyItemVariableException("Variables cannot have different parent nodes in the same context.");
                            }

                            vars[i] = v_global;
                        }
                        else if (v_global.Name == v_local.Name)
                        {
                            throw new KeyItemVariableException("Duplicate variable name");
                        }

                    }

                    if(vars[i++] == v_local)
                        variables_global_ctx.Add(v_local);

                }               
            }

            KeyItem item = new KeyItem(key, null);

            item.Context = ctx;
            item.Variables = vars;
            item._parent = KeyItem.Create(p, ctx, depth + 1);
            item._path = path;

            if (depth == 0)
            {

                string parent = item._parent != null ? item._parent.Path : "";

                Key[] keys = Cache.ContainsKey(parent) ? Cache[parent] : Store.Get(ctx).Keys(parent);

                if (!Cache.ContainsKey(parent))
                    while(!Cache.TryAdd(parent, keys));

                foreach (Key k in keys)
                {
                    if (item.Name == k.Name)
                    {
                        item = new KeyItem(key, k.Type)
                        {
                            Context = ctx,
                            Variables = vars.Count > 0 ? vars : null,
                            _parent = item._parent
                        };
                    }
                }
            }
            
            foreach (Variable v in vars)
            {
                v.PropertyChanged -= item.V_PropertyChanged;
                v.PropertyChanged += item.V_PropertyChanged;
            }

            return item;
        }

        private void V_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged("Path");
            OnPropertyChanged("Name");
        }        

        /// <summary>
        /// Copy properties to other item
        /// </summary>
        /// <param name="other"></param>
        public virtual void CopyTo(IKeyItem other)
        {
            this.Conversion.CopyTo(other.Conversion);
            this.Title.CopyTo(other.Title);
            other.Color = this.Color;
           
        }

        /// <summary>
        /// Reloads the children
        /// </summary>
        public virtual void Reload()
        {

            _children.Clear();

            foreach (Key k in Store.Get(Dispatcher.CurrentDispatcher).Keys(Path))
            {
                _children.Add(new KeyItem(k) { _parent = this });
            }

        }

        /// <summary>
        /// <see cref="IReload.Next"/>
        /// </summary>
        /// <returns></returns>
        public virtual bool Next()
        {
            return false;
        }

        /// <summary>
        /// <see cref="IReload.Previous"/>
        /// </summary>
        /// <returns></returns>
        public virtual bool Previous()
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator == (KeyItem a, KeyItem b)
        {
            if (System.Object.ReferenceEquals(a,b)) return true;
            if (((object)a) == null || ((object)b) == null) return false;

            return a.StaticPath == b.StaticPath;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator !=(KeyItem a, KeyItem b)
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

            if (obj is KeyItem)
                return ((KeyItem)obj).StaticPath == this.StaticPath;

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

        public virtual void Dispose()
        {
            if(Variables != null)
                foreach (Variable v in Variables)
                {
                    v.PropertyChanged -= V_PropertyChanged;
                }
        }
    }
}
