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

        private WindowState? _old_windowstate;

        public bool IsFullscreen
        {
            get { return (bool)GetValue(IsFullscreenProperty); }
            set { SetValue(IsFullscreenProperty, value); }
        }
        public static readonly DependencyProperty IsFullscreenProperty =
            DependencyProperty.Register("IsFullscreen", typeof(bool), typeof(Window), new FrameworkPropertyMetadata(false, (sender, e) =>
            {

                Window w = sender as Window;

                if ((bool)e.NewValue)
                {
                    w._old_windowstate = w.WindowState;
                    w.WindowState = WindowState.Maximized;
                    
                }
                else
                {
                    if(w._old_windowstate.HasValue)
                        w.WindowState = w._old_windowstate.Value;

                }

            }));


        public bool CanEnterFullscreen
        {
            get { return (bool)GetValue(CanEnterFullscreenProperty); }
            set { SetValue(CanEnterFullscreenProperty, value); }
        }
        public static readonly DependencyProperty CanEnterFullscreenProperty =
            DependencyProperty.Register("CanEnterFullscreen", typeof(bool), typeof(Window), new PropertyMetadata(false));


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
