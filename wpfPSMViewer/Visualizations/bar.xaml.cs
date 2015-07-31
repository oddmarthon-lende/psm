using System;
using System.Collections.Generic;
using System.Linq;
using OxyPlot.Series;
using OxyPlot.Axes;
using PSMViewer.Models;
using PSMViewer.ViewModels;
using OxyPlot;
using System.Windows.Data;

namespace PSMViewer.Visualizations
{

    public sealed partial class Bar : BarBase<BarSeries, BarItem>
    {

        public static string DisplayName { get { return typeof(Bar).Name; } }
        public static string Icon { get; private set; } = "../icons/chart_bar.png";

        public Bar()
        {

            Model.Axes[0].Position = AxisPosition.Left;
            Model.Axes[0].Angle = -45;
            Model.Axes[1].Position = AxisPosition.Bottom;
            Model.Axes[1].Angle = 45;

            InitializeComponent();

        }

    }
    
}
