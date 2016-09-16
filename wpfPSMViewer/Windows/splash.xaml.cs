using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace PSMViewer
{
    /// <summary>
    /// Interaction logic for splash.xaml
    /// </summary>
    public partial class Splash : PSMonitor.Theme.Window
    {

        List<Task> _tasks = new List<Task>();
        List<IReload> _objects = new List<IReload>();

        public Splash()
        {

            InitializeComponent();

            MainWindow main = new MainWindow() { IsEnabled = false };
            App.Current.MainWindow = main;

            _objects.Add(main);

            Loaded += delegate
            {

                main.Loaded += delegate
                {

                    _tasks.Add(new Task(delegate
                    {

                        main.Dispatcher.Invoke(main.Reload);

                        foreach (WindowInfo info in main.Windows)
                        {

                            info.Window.Dispatcher.Invoke(delegate
                            {
                                info.Window.IsEnabled = false;
                            });

                            Task t = new Task(delegate
                            {
                                info.Window.Dispatcher.Invoke(((VisualizationWindow)info.Window).Reload);                        
                            });

                            _objects.Add((VisualizationWindow)info.Window);                            

                            _tasks.Add(t);

                            t.Start();
                        }

                    }));

                    foreach (Task t in _tasks)
                    {
                        t.Start();
                    }

                    new Task(delegate
                    {
                        
                        Task.WaitAll(_tasks.ToArray());
                        
                        while(true)
                        {
                            int i = 0;
                            foreach (IReload r in _objects)
                            {
                                if ((ReloadStatus)((DispatcherObject)r).GetValue("Status") != ReloadStatus.Idle)
                                    i++;
                            }

                            if (i == 0)
                                break;

                            Thread.Sleep(1000);
                        }

                        Dispatcher.Invoke(Close);

                        foreach(Window w in _objects)
                        {
                            w.Dispatcher.Invoke(delegate
                            {
                                w.IsEnabled = true;
                            });
                        }

                    }).Start();

                };

                main.Show();
            };
            
        }
    }
}
