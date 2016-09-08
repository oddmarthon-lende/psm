using OxyPlot.Series;
using System.Windows.Data;
using OxyPlot;
using System.Collections.Generic;
using Xceed.Wpf.Toolkit.PropertyGrid;
using PSMViewer.ViewModels;
using System.Collections.ObjectModel;
using PSMonitor;
using OxyPlot.Axes;
using System;
using PSMViewer.Models;
using System.Timers;
using SciChart.Charting.Model.DataSeries;
using System.Windows;

namespace PSMViewer.Visualizations
{
    

    [Visible(true)]
    [DisplayName("Linechart (SciChart)")]
    [Icon("Path to icon image file")]
    [SubCategory("Subcategory (In the menu)")]
    public sealed partial class SciChartLine : VisualizationControl
    {
        // Data Sample Rate (sec)  - 20 Hz
        private const double dt = 0.05;

        // FIFO Size is 100 samples, meaning after 100 samples have been appended, each new sample appended
        // results in one sample being discarded
        private const int FifoSize = 10000;

        // Timer to process updates
        private readonly Timer _timerNewDataUpdate;

        // The current time
        private double t;

        // The dataseries to fill
        private IXyDataSeries<double, double> _series0;
        private IXyDataSeries<double, double> _series1;
        private IXyDataSeries<double, double> _series2;

        public SciChartLine()
        {
            
            InitializeComponent();

            _timerNewDataUpdate = new Timer(dt * 1000);
            _timerNewDataUpdate.AutoReset = true;
            _timerNewDataUpdate.Elapsed += OnNewData;

            // Create new Dataseries of type X=double, Y=double
            _series0 = new XyDataSeries<double, double>() { FifoCapacity = FifoSize, SeriesName = "Orange Series" };
            _series1 = new XyDataSeries<double, double>() { FifoCapacity = FifoSize, SeriesName = "Blue Series" };
            _series2 = new XyDataSeries<double, double>() { FifoCapacity = FifoSize, SeriesName = "Green Series" };

            // Set the dataseries on the chart's RenderableSeries
            renderableSeries0.DataSeries = _series0;
            renderableSeries1.DataSeries = _series1;
            renderableSeries2.DataSeries = _series2;

            Loaded += OnExampleLoaded;
        }

        private void ClearDataSeries()
        {
            if (_series0 == null)
                return;

            using (sciChartSurface.SuspendUpdates())
            {
                _series0.Clear();
                _series1.Clear();
                _series2.Clear();
            }
        }

        private void OnNewData(object sender, EventArgs e)
        {
            // Compute our three series values
            double y1 = 3.0 * Math.Sin(((2 * Math.PI) * 1.4) * t * 0.02);
            double y2 = 2.0 * Math.Cos(((2 * Math.PI) * 0.8) * t * 0.02);
            double y3 = 1.0 * Math.Sin(((2 * Math.PI) * 2.2) * t * 0.02);

            // Suspending updates is optional, and ensures we only get one redraw
            // once all three dataseries have been appended to
            using (sciChartSurface.SuspendUpdates())
            {
                // Append x,y data to previously created series
                _series0.Append(t, y1);
                _series1.Append(t, y2);
                _series2.Append(t, y3);
            }

            // Increment current time
            t += dt;
        }

        private void OnExampleLoaded(object sender, RoutedEventArgs e)
        {
            ClearDataSeries();

            _timerNewDataUpdate.Start();
        }

        private void OnExampleUnloaded(object sender, RoutedEventArgs e)
        {
            if (_timerNewDataUpdate != null)
            {
                _timerNewDataUpdate.Stop();
            }
        }

    }
}
