using PSMViewer.Models;
using PSMViewer.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PSMViewer
{
    
    public partial class Tree : TreeView, IReload
    {
        public Tree()
        {
            InitializeComponent();

            AddHandler(TreeViewItem.ExpandedEvent, new RoutedEventHandler(Reload));

        }

        public Window Window
        {
            get
            {
                Window window = (Window)Resources["window"];
                window.Content = this;

                Reload();

                return window;
            }
            
        }

        public bool Next()
        {
            return false;
        }

        public bool Previous()
        {
            return false;
        }

        public void Reload()
        {
            ((KeyItem)rootItem.DataContext).Reload();
        }

        private KeyItem GetDataContext(RoutedEventArgs e)
        {

            FrameworkElement item = null;
            KeyItem key = null;

            try
            {
                item = (FrameworkElement)e.OriginalSource;
            }
            catch (Exception) { }

            if (item == null) return null;

            try
            {
                key = (KeyItem)item.DataContext;
            }
            catch (Exception) { }

            if (key == null) return null;

            return key;
        }

        private void Reload(object sender, RoutedEventArgs e)
        {
            GetDataContext(e).Reload();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            ((Window)sender).Hide();
        }
    }
}
