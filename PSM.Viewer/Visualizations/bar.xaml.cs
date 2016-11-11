using OxyPlot.Axes;
using PSM.Viewer.Models;
using System;

namespace PSM.Viewer.Visualizations
{
    /// <summary>
    /// 
    /// </summary>
    public class BarItem : OxyPlot.Series.BarItem, IEntry
    {

        /// <summary>
        /// 
        /// </summary>
        public Index Index { get; set; }

        /// <summary>
        /// 
        /// </summary>
        object IEntry.Value
        {
            get
            {
                return base.Value;
            }
        }
                
        /// <summary>
        /// 
        /// </summary>
        public BarItem() : base()
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="categoryIndex"></param>
        public BarItem(double value, int categoryIndex = -1) : base(value, categoryIndex) { }

    }

    /// <summary>
    /// 
    /// </summary>
    [Visible(true)]
    [DisplayName("Bars (OxyPlot)")]
    [Icon("../icons/chart_bar.png")]
    [SubCategory("OxyPlot")]
    public sealed partial class Bar : BarBase<OxyPlot.Series.BarSeries, BarItem>
    {

        /// <summary>
        /// Constructor
        /// </summary>
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
