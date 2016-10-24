using System;
/// <copyright file="ikeyitem.cs" company="Baker Hughes Incorporated">
/// Copyright (c) 2015 All Rights Reserved
/// </copyright>
/// <author>Odd Marthon Lende</author>
/// 
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Media;

namespace PSM.Viewer.Models
{
    public interface IKeyItem : IReload, INotifyPropertyChanged, IDisposable
    {
        /// <summary>
        /// 
        /// </summary>
        KeyItemValueConversion Conversion { get; }

        /// <summary>
        /// 
        /// </summary>
        KeyItemTitle Title { get; }

        /// <summary>
        /// 
        /// </summary>
        string Path { get; }

        /// <summary>
        /// 
        /// </summary>
        string StaticPath { get; }

        /// <summary>
        /// 
        /// </summary>
        Color Color { get; set; }

        /// <summary>
        /// 
        /// </summary>
        SolidColorBrush Brush { get; }
        
        /// <summary>
        /// 
        /// </summary>
        KeyItemW W { get; }

        /// <summary>
        /// 
        /// </summary>
        ObservableCollection<IKeyItem> Children { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="other"></param>
        void CopyTo(IKeyItem other);
    }
}
