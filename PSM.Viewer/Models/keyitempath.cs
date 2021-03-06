﻿/// <copyright file="keyitempath.cs" company="Baker Hughes Incorporated">
/// Copyright (c) 2015 All Rights Reserved
/// </copyright>
/// <author>Odd Marthon Lende</author>
/// 
using PSM.Stores;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Markup;
using System.Windows.Media;

namespace PSM.Viewer.Models
{
    /// <summary>
    /// 
    /// </summary>
    [ContentProperty("Children")]
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
        /// Convert to <see cref="KeyItem"/>
        /// </summary>
        /// <param name="p"></param>
        /// <returns>A new <see cref="KeyItem"/></returns>
        public KeyItem ToKeyItem()
        {

            KeyItem key = KeyItem.Create(Path);

            CopyTo(key);

            foreach(KeyItemPath p in Children)
            {
                key.Children.Add(p.ToKeyItem());
            }

            return key;
        }

        /// <summary>
        /// Convert to <see cref="KeyItemW"/>
        /// </summary>
        /// <param name="p"></param>
        /// <returns>A new <see cref="KeyItem"/></returns>
        public KeyItemW ToKeyItemW()
        {

            KeyItemW key = KeyItemW.Create(Path);

            CopyTo(key);
            key.Memory = Children;

            return key;
        }

        /// <summary>
        /// 
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public List<KeyItemPath> Children { get; private set; } = new List<KeyItemPath>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        public KeyItemPath(KeyItemW key) : this((IKeyItem)key)
        {
            Children = key.Memory.ToList();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        public KeyItemPath(IKeyItem key)
        {
            
            Path = key.StaticPath;
            Position = key.Title.Position;
            Conversion.Mode = key.Conversion.Mode;
            Conversion.Value = key.Conversion.Value;
            Mode = key.Title.Mode;
            Alias = key.Title.Alias;
            Children = key.Children.Select((k) => new KeyItemPath(k)).ToList();

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        public void CopyTo(IKeyItem key)
        {

            if (Position.HasValue)
                key.Title.Position = Position.Value;

            key.Conversion.Mode = Conversion.Mode;
            key.Conversion.Value = Conversion.Value;
            key.Title.Mode = Mode;
            key.Title.Alias = Alias;
        }

        /// <summary>
        /// 
        /// </summary>
        public KeyItemPath() { }


    }

    /// <summary>
    /// A wrapper class around List<string>.
    /// Used to hold the key paths when the VisualizationControl is serialized to XAML
    /// </summary>
    public class KeyItemPathList : List<KeyItemPath>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public bool Contains(string path)
        {

            foreach (KeyItemPath p in this)
                if (p.Path == path)
                    return true;

            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public KeyItemPath Get(string path)
        {

            foreach (KeyItemPath p in this)
                if (p.Path == path)
                    return p;

            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public bool Remove(string path)
        {

            foreach (KeyItemPath p in this)
                if (p.Path == path)
                    return Remove(p);

            return false;
        }

    }
}
