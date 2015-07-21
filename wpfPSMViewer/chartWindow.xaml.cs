using PSMViewer.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Timers;
using System.Windows;
using System.Windows.Controls;

namespace PSMViewer
{
    /// <summary>
    /// Interaction logic for chartWindow.xaml
    /// </summary>
    public partial class ChartWindow : Window, IDisposable
    {
        public UIElementCollection Charts
        {
            get
            {
                return grid.Children;
            }
        }

        private Timer timer = new Timer(1000);

        public double Interval
        {
            get
            {
                return timer.Interval;
            }
            set
            {
                timer.Interval = value;
            }
        }

        public ChartWindow()
        {
            InitializeComponent();
            
            timer.Elapsed += Reload;
            timer.Start();
        }

        public void Dispose()
        {
            timer.Elapsed -= Reload;
            timer.Dispose();
        }

        public Chart Get(string title)
        {

            Chart[] elements = new Chart[Charts.Count];
            Charts.CopyTo(elements, 0);

            return (from c in elements where (string)c.Title == title select c).ElementAtOrDefault(0);
        }

        public Chart Add(string title)
        {
             Chart chart = Get(title) ?? new Chart() { Title = title };

            if(!Charts.Contains(chart))
                Charts.Add(chart);

            return chart;
        }

        public void Reload(object sender, ElapsedEventArgs e)
        {
            Dispatcher.Invoke(Reload);
        }

        public void Reload()
        {
            foreach(Chart chart in Charts)
            {
                chart.Reload();
            }
        }

        public override string ToString()
        {
            return this.Title;
        }
        
    }
}
