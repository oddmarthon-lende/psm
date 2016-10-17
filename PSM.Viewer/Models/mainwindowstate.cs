/// <copyright file="mainwindowstate.cs" company="Baker Hughes Incorporated">
/// Copyright (c) 2015 All Rights Reserved
/// </copyright>
/// <author>Odd Marthon Lende</author>
/// 

using System.Windows;

namespace PSM.Viewer.Models
{

    /// <summary>
    /// Mainwindow properties to serialize
    /// </summary>
    public class MainWindowSavedState : WindowSavedState
    {

        public Settings Options { get; set; } = null;

        public Visibility[] ToolbarsVisibility { get; set; }

        public MainWindowSavedState() : base() { }

        public MainWindowSavedState(MainWindow window) : base(window)
        {
            Options = window.Options;
            ToolbarsVisibility = window.ToolbarsVisibility;
        }

        public void Restore(MainWindow window)
        {
            if (Options != null)
            {
                window.Options = Options;
            }

            if(ToolbarsVisibility != null)
            {
                window.ToolbarsVisibility = ToolbarsVisibility;
            }

            base.Restore(window);
        }

    }
}
