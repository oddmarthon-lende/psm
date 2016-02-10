/// <copyright file="windowstate.cs" company="Baker Hughes Incorporated">
/// Copyright (c) 2015 All Rights Reserved
/// </copyright>
/// <author>Odd Marthon Lende</author>
/// <summary></summary>
/// 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace PSMViewer
{

    /// <summary>
    /// Used for serialization of window state properties of intrest
    /// </summary>
    public class WindowSavedState
    {

        public double Left { get; set; } = 0D;
        public double Top { get; set; } = 0D;
        public double Width { get; set; } = 800D;
        public double Height { get; set; } = 600D;
        public WindowState WindowState { get; set; } = WindowState.Normal;

        public WindowSavedState() { }

        public WindowSavedState(Window window)
        {
            Left = window.Left;
            Top = window.Top;
            Width = window.Width;
            Height = window.Height;
            WindowState = window.WindowState;
        }

        public virtual void Restore(Window window)
        {
            window.Left   = Left;
            window.Top    = Top;
            window.Width  = Width;
            window.Height = Height;
            window.WindowState = WindowState;
        }
    }
}
