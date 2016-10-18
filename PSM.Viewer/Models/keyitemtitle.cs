/// <copyright file="keyitemtitle.cs" company="Baker Hughes Incorporated">
/// Copyright (c) 2015 All Rights Reserved
/// </copyright>
/// <author>Odd Marthon Lende</author>
/// 
using System;
using System.ComponentModel;

namespace PSM.Viewer.Models
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

        private KeyItemTitleMode? _mode;
        /// <summary>
        /// 
        /// </summary>
        public KeyItemTitleMode Mode
        {
            get
            {
                return _mode.HasValue ? _mode.Value : Key.W != null ? Key.W.Title.Mode : KeyItemTitleMode.Full;
            }

            set
            {
                _mode = value;

                if(Key.W != null && Key.W.Title._mode == _mode)
                {
                    _mode = null;
                }

                foreach(KeyItem key in Key.Children)
                {
                    if(!key.Title._mode.HasValue)
                    {
                        key.Title.OnPropertyChanged("Mode");
                        key.Title.OnPropertyChanged("Value");
                    }
                }

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
                return _alias == null ? Key.W != null ? Key.W.Title.Alias : null : _alias;
            }

            set
            {
                _alias = value;

                if(Key.W != null && _alias == Key.W.Title._alias)
                {
                    _alias = null;
                }

                foreach (KeyItem key in Key.Children)
                {
                    if (key.Title._alias == null)
                    {
                        key.Title.OnPropertyChanged("Alias");
                        key.Title.OnPropertyChanged("Value");
                    }
                }

                OnPropertyChanged("Alias");
                OnPropertyChanged("Value");
            }
        }        

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

        private uint? _position = null;
        /// <summary>
        /// 
        /// </summary>
        public uint Position
        {

            get
            {

                string[] components = GetComponents();
                return _position.HasValue ? _position.Value : Key.W != null ? Key.W.Title.Position : ((uint)(components.Length - 1));
            }

            set
            {

                string[] components = GetComponents();
                _position = Math.Min(value, ((uint)(components.Length - 1)));

                if (Key.W != null && _position == Key.W.Title._position)
                {
                    _position = null;
                }

                foreach (KeyItem key in Key.Children)
                {
                    if (!key.Title._position.HasValue)
                    {
                        key.Title.OnPropertyChanged("Position");
                        key.Title.OnPropertyChanged("Value");
                    }
                }

                OnPropertyChanged("Position");
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
