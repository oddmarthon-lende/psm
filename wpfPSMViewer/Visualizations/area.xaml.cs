using OxyPlot.Series;

namespace PSMViewer.Visualizations
{

    public sealed partial class Line : OxyBase<TwoColorLineSeries>
    {
        public static string DisplayName { get { return typeof(Line).Name; } }
        public static string Icon { get { return @"..\icons\chart_curve.png"; } }

        public Line()
        {
            InitializeComponent();
        }
    }
}
