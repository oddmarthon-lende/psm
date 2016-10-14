using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSMViewer.Models
{
    // How to display the title
    public enum KeyItemTitleMode
    {
        Alias,
        Full,
        Component
    }

    /// <summary>
    /// The <see cref="KeyItem"/> title property
    /// </summary>
    public class KeyItemTitle : INotifyPropertyChanged
    {

        private KeyItemTitleMode _mode = KeyItemTitleMode.Component;
        /// <summary>
        /// 
        /// </summary>
        public KeyItemTitleMode Mode
        {
            get
            {
                return _mode;
            }

            set
            {
                _mode = value;
                OnPropertyChanged("Mode");
                OnPropertyChanged("Value");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public IKeyItem Key { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public string Value
        {
            get
            {
                switch (Mode)
                {

                    case KeyItemTitleMode.Alias:
                        return Alias;

                    case KeyItemTitleMode.Component:
                        return GetComponents()[Position];

                    case KeyItemTitleMode.Full:
                        return Key.Path.ToLower();

                }

                return null;

            }

        }

        private string _alias;
        /// <summary>
        /// 
        /// </summary>
        public string Alias
        {
            get
            {
                return _alias;
            }

            set
            {
                _alias = value;
                OnPropertyChanged("Alias");
                OnPropertyChanged("Value");
            }
        }

        private uint? _position = null;

        /// <summary>
        /// 
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 
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
        public uint Position
        {

            get
            {

                string[] components = GetComponents();
                return _position.HasValue ? _position.Value : ((uint)(components.Length - 1));
            }

            set
            {

                string[] components = GetComponents();
                _position = Math.Min(value, ((uint)(components.Length - 1)));

                OnPropertyChanged("Position");
                OnPropertyChanged("Name");
                OnPropertyChanged("Value");
            }

        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="item"></param>
        public KeyItemTitle(IKeyItem item)
        {
            Key = item;
            item.PropertyChanged += Item_PropertyChanged;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="other"></param>
        public KeyItemTitle(KeyItemTitle other) : this(other.Key)
        {
            this.Mode = other.Mode;
            this.Alias = other.Alias;
            this.Position = other.Position;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch(Mode)
            {
                case KeyItemTitleMode.Component:
                case KeyItemTitleMode.Full:
                    OnPropertyChanged("Value");
                    break;
            }
            
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private string[] GetComponents()
        {
            return Key.Path.Split('.');
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Value;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator ==(KeyItemTitle a, KeyItemTitle b)
        {
            if (System.Object.ReferenceEquals(a, b)) return true;
            if (((object)a) == null || ((object)b) == null) return false;

            return a.Value == b.Value;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator !=(KeyItemTitle a, KeyItemTitle b)
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

            if (obj is KeyItemTitle)
                return ((KeyItemTitle)obj).Value == this.Value;

            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="other"></param>
        public void CopyTo(KeyItemTitle other)
        {
            other.Mode = this.Mode;
            other.Alias = this.Alias;
            other.Position = this.Position;
        }

    }

    /// <summary>
    /// 
    /// </summary>
    public class KeyItemVariableException : Exception
    {
        public KeyItemVariableException(string msg) : base(msg)
        {

        }
    }
}
