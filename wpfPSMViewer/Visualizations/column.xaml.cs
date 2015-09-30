using OxyPlot.Series;

namespace PSMViewer.Visualizations
{
    [Visible(true)]
    [DisplayName("Columns (OxyPlot)")]
    [Icon("../icons/chart_column.png")]
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
