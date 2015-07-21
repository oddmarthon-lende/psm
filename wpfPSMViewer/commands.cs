using System.Windows.Input;

namespace PSMViewer
{
    public static class Commands
    {
        public static readonly RoutedUICommand Next     = new RoutedUICommand("Load next results", "Next", typeof(MainWindow));
        public static readonly RoutedUICommand Previous = new RoutedUICommand("Load previous results", "Previous", typeof(MainWindow));
        public static readonly RoutedUICommand Refresh  = new RoutedUICommand("Reload results", "Refresh", typeof(MainWindow));
        public static readonly RoutedUICommand Exit = new RoutedUICommand("Exit the program", "Exit", typeof(MainWindow));
        public static readonly RoutedUICommand About = new RoutedUICommand("About this program", "About", typeof(MainWindow));
        public static readonly RoutedUICommand ContextMenuClick = new RoutedUICommand("Context Menu Clicked", "ContextMenuClick", typeof(MainWindow));
    }
}
