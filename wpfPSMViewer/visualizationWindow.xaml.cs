using PSMViewer.Models;
using System;
using System.Linq;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using PSMViewer.Visualizations;
using System.Reflection;
using PSMViewer.Properties;
using System.Collections.Specialized;

namespace PSMViewer
{
    /// <summary>
    /// Interaction logic for chartWindow.xaml
    /// </summary>
    public partial class VisualizationWindow : Window, IDisposable
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

        public VisualizationWindow()
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

        public VisualizationControl Get(string title)
        {

            VisualizationControl[] elements = new VisualizationControl[Charts.Count];
            Charts.CopyTo(elements, 0);

            return (from c in elements where (string)c.Title == title select c).ElementAtOrDefault(0);
        }
        
        public VisualizationControl Add(string title)
        {

            VisualizationControl chart = Get(title) ?? new Text() { Title = title };

            if(!Charts.Contains(chart))
                Charts.Add(chart);

            return chart;
        }

        public void Reload(object sender, ElapsedEventArgs e)
        {
            Reload();
        }

        public void Reload()
        {
            foreach(VisualizationControl chart in Charts)
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
