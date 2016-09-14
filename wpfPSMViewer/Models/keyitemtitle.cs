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

        public IKeyItem Key { get; private set; }

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

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

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

        public KeyItemTitle(IKeyItem item)
        {
            Key = item;
            item.PropertyChanged += Item_PropertyChanged;
        }

        public KeyItemTitle(KeyItemTitle other) : this(other.Key)
        {
            this.Mode = other.Mode;
            this.Alias = other.Alias;
            this.Position = other.Position;
        }

        private void Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged("Name");
        }

        private string[] GetComponents()
        {
            return Key.Path.Split('.');
        }

        public override string ToString()
        {
            return Value;
        }

        public static bool operator ==(KeyItemTitle a, KeyItemTitle b)
        {
            if (System.Object.ReferenceEquals(a, b)) return true;
            if (((object)a) == null || ((object)b) == null) return false;

            return a.Value == b.Value;
        }

        public static bool operator !=(KeyItemTitle a, KeyItemTitle b)
        {
            return !(a == b);
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            if (obj is KeyItemTitle)
                return ((KeyItemTitle)obj).Value == this.Value;

            return false;
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public void CopyTo(KeyItemTitle other)
        {
            other.Mode = this.Mode;
            other.Alias = this.Alias;
            other.Position = this.Position;
        }

    }

    public class KeyItemVariableException : Exception
    {
        public KeyItemVariableException(string msg) : base(msg)
        {

        }
    }
}
