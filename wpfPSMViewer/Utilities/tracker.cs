/// <copyright file="tracker.cs" company="Baker Hughes Incorporated">
/// Copyright (c) 2015 All Rights Reserved
/// </copyright>
/// <author>Odd Marthon Lende</author>
/// <summary>Tracks and detects the element that is under the mouse</summary>
/// 

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace PSMViewer.Utilities
{

    /// <summary>
    /// Tracks and detects the element that is under the mouse
    /// </summary>
    public class Tracker : IDisposable
    {

        /// <summary>
        /// The elements to check
        /// </summary>
        private IEnumerable<Control> Elements;

        /// <summary>
        /// The current <see cref="Control"/> that is under the mouse
        /// </summary>
        private Control Current = null;

        /// <summary>
        /// The window that contains the <see cref="Elements"/>
        /// </summary>
        private Window Window;

        /// <summary>
        /// The delegate that is called when the mouse moves over an element
        /// </summary>
        public event Action<Control> MouseOver;

        /// <summary>
        /// The delegate that is called when the mouse moves away from the <see cref="Current"/> selected element.
        /// </summary>
        public event Action<Control> MouseOut;

        /// <summary>
        /// The delegate that gets called when the mouse is clicked
        /// </summary>
        public event Action<Control, MouseButtonEventArgs> MouseButtonUp;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="window"><see cref="Window"/></param>
        /// <param name="elements"><see cref="Elements"/></param>
        public Tracker(Window window, IEnumerable<Control> elements)
        {
            this.Elements = elements;
            this.Window = window;

            Window.PreviewMouseMove          += Window_PreviewMouseMove;
            Window.PreviewMouseLeftButtonUp  += Window_PreviewMouseButtonUp;
            Window.PreviewMouseRightButtonUp += Window_PreviewMouseButtonUp;

            Dispatcher.CurrentDispatcher.InvokeAsync(delegate
            {
                Window_PreviewMouseMove(Window, new MouseEventArgs(Mouse.PrimaryDevice, (int)(DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0)).TotalMilliseconds));
            });

        }

        /// <summary>
        /// Event handler
        /// </summary>
        /// <param name="sender">The sending window</param>
        /// <param name="e">Event arguments</param>
        private void Window_PreviewMouseButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (MouseButtonUp != null)
                MouseButtonUp(Current, e);
        }

        /// <summary>
        /// Event handler
        /// </summary>
        /// <param name="sender">The sending window</param>
        /// <param name="e">Event arguments</param>
        private void Window_PreviewMouseMove(object sender, MouseEventArgs e)
        {

            Window w = (Window)sender;

            foreach (Control element in Elements)
            {

                if (VisualTreeHelper.GetDescendantBounds(element).Contains(e.GetPosition(element)))
                {
                    if(Current != element)
                    {

                        if (MouseOut != null && Current != null)
                            MouseOut(Current);

                        Current = element;

                        if(MouseOver != null)
                            MouseOver(Current);
                    }
                    
                }
            }
        }

        /// <summary>
        /// Releases and cleans up any resources created
        /// </summary>
        public void Dispose()
        {
            Window.PreviewMouseMove -= Window_PreviewMouseMove;
            Window.PreviewMouseLeftButtonUp -= Window_PreviewMouseButtonUp;
            Window.PreviewMouseRightButtonUp -= Window_PreviewMouseButtonUp;
        }
    }
}
