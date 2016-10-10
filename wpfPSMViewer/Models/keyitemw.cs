using PSMonitor;
using PSMonitor.Stores;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows.Media;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PSMViewer.Models
{
    public class KeyItemW : IKeyItem, INotifyPropertyChanged, IDisposable
    {

        /// <summary>
        /// 
        /// </summary>
        public KeyItemW W { get; set; } = null;

        /// <summary>
        /// 
        /// </summary>
        public KeyValueConversion Conversion { get; private set; } = new KeyValueConversion();

        KeyItemTitle _title;
        /// <summary>
        /// 
        /// </summary>
        public KeyItemTitle Title
        {
            get
            {
                return _title;
            }
        }

        private string _path;
        /// <summary>
        /// 
        /// </summary>
        public string Path
        {
            get
            {
                return _path;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public string StaticPath
        {
            get
            {
                return _path;
            }
        }

        private Color _color = Colors.Black;
        /// <summary>
        /// 
        /// </summary>
        public Color Color
        {
            get
            {
                return _color;
            }

            set
            {
                _color = value;
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
                return new SolidColorBrush(_color);
            }
        }

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

        public bool HasWildcards { get; private set; } = false;

        /// <summary>
        /// 
        /// </summary>
        public CancellationTokenSource CancellationTokenSource { get; set; } = new CancellationTokenSource();

        private ObservableCollection<IKeyItem> _children = new ObservableCollection<IKeyItem>();
        /// <summary>
        ///
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
        public Dispatcher Dispatcher { get; private set; } = Dispatcher.CurrentDispatcher;

        /// <summary>
        /// 
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TField"></typeparam>
        /// <param name="field"></param>
        /// <param name="value"></param>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        protected bool SetField<TField>(ref TField field, TField value, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<TField>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);

            return true;
        }

        private System.Timers.Timer _timer = new System.Timers.Timer(15000);

        /// <summary>
        /// Constructor
        /// </summary>
        public KeyItemW()
        {

            _title = new KeyItemTitle(this);
            _timer.Elapsed += timer_Elapsed;            
            
        }

        private void timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            this.OnReload(this, Logger.Error);
        }

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
        /// 
        /// </summary>
        public ReloadStatus Status { get; set; } = ReloadStatus.Idle;
        
        /// <summary>
        /// Create a new wildcard item
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static KeyItemW Create(string path)
        {

            List<IKeyItem> keys = new List<IKeyItem>();
            Path p = PSMonitor.Path.Extract(KeyItem.Create(path).Path);
            int i = 0;
            List<string> components = p.Components.ToList();
            KeyItemW item = new KeyItemW() { _path = path };
            bool _w = false;

            if (path.IndexOf('*') == -1)
                keys.Add(KeyItem.Create(path));

            foreach (string c in components.ToArray())
            {

                if (c.Contains("*"))
                {

                    string ns = String.Join(".", components.GetRange(0, i));
                    Wildcard wildcard = new Wildcard(c, RegexOptions.IgnoreCase);

                    foreach (Key k in PSM.Store(Dispatcher.CurrentDispatcher).Keys(ns))
                    {
                        if (!wildcard.IsMatch(k.Name))
                            continue;
                        else if (k.Type != null && i < components.Count - 1)
                            continue;
                        else if (k.Type == null && i == components.Count - 1)
                            continue;

                        components[i] = k.Name;
                        keys.AddRange(KeyItemW.Create(String.Join(".", components)).Children);

                    }

                    _w = true;
                }

                i++;
            }

            item.HasWildcards = _w;

            foreach (IKeyItem k in keys)
            {
                k.W = _w ? item : null;
                item.Children.Add(k);
            }
            
            return item;
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
        /// <see cref="IReload.Reload"/>
        /// </summary>
        public void Reload()
        {

            KeyItemW item = KeyItemW.Create(Path);

            foreach (KeyItem k in item.Children)
            {
                if (!Children.Contains(k))
                    Children.Add(k);
            }

            foreach (KeyItem k in Children)
            {
                if (!item.Children.Contains(k))
                    Children.Remove(k);
            }

        }

        /// <summary>
        /// <see cref="IDisposable.Dispose"/>
        /// </summary>
        public void Dispose()
        {
            _timer.Dispose();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="other"></param>
        public void CopyTo(IKeyItem other)
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
