using OxyPlot.Series;

namespace PSM.Viewer.Visualizations
{
    [DisplayName("Linear Bar Graph")]
    [SubCategory("OxyPlot")]
    public sealed partial class LinearBars : OxyBase<LinearBarSeries>
    {
        
        public LinearBars()
        {
            InitializeComponent();
        }
    }
}
