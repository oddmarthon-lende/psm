using OxyPlot.Series;

namespace PSMViewer.Visualizations
{

    public sealed partial class Column : BarBase<ColumnSeries, ColumnItem>
    {

        public static string DisplayName { get { return typeof(Column).Name; } }
        public static string Icon { get; private set; } = "../icons/chart_column.png";

        public Column()
        {
            InitializeComponent();
        }

    }
    
}
