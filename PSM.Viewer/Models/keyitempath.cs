using PSM.Stores;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace PSM.Viewer.Models
{
    /// <summary>
    /// 
    /// </summary>
    public class KeyItemPath
    {
        /// <summary>
        /// 
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public uint? Position { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public Color? Color { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public KeyValueConversion Conversion { get; set; } = new KeyValueConversion();

        /// <summary>
        /// 
        /// </summary>
        public KeyItemTitleMode Mode { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string Alias { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string W { get; set; }

        /// <summary>
        /// Convert to <see cref="KeyItem"/>
        /// </summary>
        /// <param name="p"></param>
        /// <returns>A new <see cref="KeyItem"/></returns>
        public static KeyItem ToKeyItem(KeyItemPath p)
        {

            KeyItem key = KeyItem.Create(p.Path);

            if (p.Position.HasValue)
                key.Title.Position = p.Position.Value;

            key.Color = p.Color.Value;
            key.Conversion.Mode = p.Conversion.Mode;
            key.Conversion.Value = p.Conversion.Value;
            key.Title.Mode = p.Mode;
            key.Title.Alias = p.Alias;

            return key;
        }

        /// <summary>
        /// Convert to <see cref="KeyItemW"/>
        /// </summary>
        /// <param name="p"></param>
        /// <returns>A new <see cref="KeyItem"/></returns>
        public static KeyItemW ToKeyItemW(KeyItemPath p)
        {

            KeyItemW key = KeyItemW.Create(p.Path);

            if (p.Position.HasValue)
                key.Title.Position = p.Position.Value;

            key.Color = p.Color.Value;
            key.Conversion.Mode = p.Conversion.Mode;
            key.Conversion.Value = p.Conversion.Value;
            key.Title.Mode = p.Mode;
            key.Title.Alias = p.Alias;

            return key;
        }

        public KeyItemPath(IKeyItem key)
        {
            Path = key.StaticPath;
            Position = key.Title.Position;
            Color = key.Color;
            Conversion.Mode = key.Conversion.Mode;
            Conversion.Value = key.Conversion.Value;
            Mode = key.Title.Mode;
            Alias = key.Title.Alias;
            W = key.W != null ? key.W.StaticPath : null;

        }

        public KeyItemPath() { }


    }

    /// <summary>
    /// A wrapper class around List<string>.
    /// Used to hold the key paths when the VisualizationControl is serialized to XAML
    /// </summary>
    public class KeyItemPathList : List<KeyItemPath>
    {

        public bool Contains(string path)
        {

            foreach (KeyItemPath p in this)
                if (p.Path == path)
                    return true;

            return false;
        }

        public KeyItemPath Get(string path)
        {

            foreach (KeyItemPath p in this)
                if (p.Path == path)
                    return p;

            return null;
        }

        public bool Remove(string path)
        {

            foreach (KeyItemPath p in this)
                if (p.Path == path)
                    return Remove(p);

            return false;
        }

    }
}
