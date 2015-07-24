using OxyPlot.Series;

namespace PSMViewer.Visualizations
{

    public sealed partial class StairStep : OxyBase<StairStepSeries>
    {
        public static string DisplayName { get { return "Stair Step"; } }
        public static string Icon { get { return @""; } }

        public StairStep()
        {
            InitializeComponent();
        }
    }
}
