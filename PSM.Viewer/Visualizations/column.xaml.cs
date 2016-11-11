using PSM.Viewer.Models;
using System;

namespace PSM.Viewer.Visualizations
{
    /// <summary>
    /// 
    /// </summary>
    public class ColumnItem : OxyPlot.Series.ColumnItem, IEntry
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
        public ColumnItem() : base()
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="categoryIndex"></param>
        public ColumnItem(double value, int categoryIndex = -1) : base(value, categoryIndex) { }
    }

    [Visible(true)]
    [DisplayName("Columns (OxyPlot)")]
    [Icon("../icons/chart_column.png")]
    [SubCategory("OxyPlot")]
    public sealed partial class Column : BarBase<OxyPlot.Series.ColumnSeries, ColumnItem>
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public Column()
        {
            InitializeComponent();                        
        }

    }
    
}
