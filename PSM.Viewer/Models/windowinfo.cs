using System.Windows;
using System.Windows.Media.Imaging;

namespace PSM.Viewer.Models
{
    /// <summary>
    /// Used to read properties from the windows objects, because they were belong to a different thread.        
    /// </summary>
    public class WindowInfo : DispatcherObjectPropertyWrapper
    {

        /// <summary>
        /// The window
        /// </summary>
        public Window Window { get; private set; }

        /// <summary>
        /// Gets the window title
        /// </summary>
        public string Title
        {

            get
            {
                return (string)Window.GetValue("Title");
            }
        }

        /// <summary>
        /// Gets the window thumbnail
        /// </summary>
        public BitmapSource Thumbnail
        {

            get
            {

                BitmapSource src = null;
                bool visible = Window.IsVisible;

                if (!visible)
                    Window.Dispatcher.Invoke(Window.Show);

                src = Window.Dispatcher.Invoke(delegate { return Window.GetThumbnail(); });

                if (!visible)
                    Window.Dispatcher.Invoke(Window.Hide);

                return src;
            }
        }

        /// <summary>
        /// The constructor
        /// </summary>
        /// <param name="window">The window to wrap</param>
        public WindowInfo(Window window) : base(window)
        {
            this.Window = window;
        }
    }
}
