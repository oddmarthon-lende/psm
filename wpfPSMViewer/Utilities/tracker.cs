using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace PSMViewer.Utilities
{
    public class Tracker : IDisposable
    {

        private IEnumerable<Control> Elements;
        private Control Current = null;
        private Window Window;

        public event Action<Control> MouseOver;
        public event Action<Control> MouseOut;
        public event Action<Control, MouseButtonEventArgs> MouseButtonUp;

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

        private void Window_PreviewMouseButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (MouseButtonUp != null)
                MouseButtonUp(Current, e);
        }

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

        public void Dispose()
        {
            Window.PreviewMouseMove -= Window_PreviewMouseMove;
            Window.PreviewMouseLeftButtonUp -= Window_PreviewMouseButtonUp;
            Window.PreviewMouseRightButtonUp -= Window_PreviewMouseButtonUp;
        }
    }
}
