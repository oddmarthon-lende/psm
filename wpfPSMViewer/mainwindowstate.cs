/// <copyright file="mainwindowstate.cs" company="Baker Hughes Incorporated">
/// Copyright (c) 2015 All Rights Reserved
/// </copyright>
/// <author>Odd Marthon Lende</author>
/// 

using PSMViewer.ViewModels;
using System;

namespace PSMViewer
{

    /// <summary>
    /// Mainwindow properties to serialize
    /// </summary>
    public class MainWindowSavedState : WindowSavedState
    {

        public Settings Options { get; set; } = null;

        public MainWindowSavedState() : base() { }

        public MainWindowSavedState(MainWindow window) : base(window)
        {
            Options = window.Options;
        }

        public void Restore(MainWindow window)
        {
            if (Options != null)
            {
                window.Options = Options;
            }

            base.Restore(window);
        }

    }
}
