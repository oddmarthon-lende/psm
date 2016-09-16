using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace PSMonitor.Theme
{
    public class Window : System.Windows.Window
    {

        public Window()
        {

            Uri uri = new Uri("pack://application:,,,/PSMonitor.Theme;component/Resources/icons.xaml", UriKind.RelativeOrAbsolute);

            Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = uri });

            uri = new Uri("pack://application:,,,/PSMonitor.Theme;component/Resources/window.xaml", UriKind.RelativeOrAbsolute);

            Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = uri });

            Template = (ControlTemplate)Resources["CustomizedWindow"];
            Style = (Style)Resources["WindowStyle"];
            BorderThickness = new Thickness(1);
            
        }
    }
}
