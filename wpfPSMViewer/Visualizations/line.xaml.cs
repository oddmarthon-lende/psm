using OxyPlot.Series;

namespace PSMViewer.Visualizations
{

    public sealed partial class Area : OxyBase<TwoColorAreaSeries>
    {
        public static string DisplayName { get { return typeof(Area).Name; } }
        public static string Icon { get { return @""; } }

        public Area()
        {
            InitializeComponent();
        }
    }
}
