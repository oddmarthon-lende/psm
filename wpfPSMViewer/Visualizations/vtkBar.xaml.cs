using Kitware.VTK;

namespace PSMViewer.Visualizations
{

    [Visible(true)]
    [DisplayName("Bar (VTK)")]
    [Icon("../icons/chart_bar.png")]
    public sealed partial class vtkChartBar : vtkChartBase
    {
               
        /// <summary>
        /// Constructor
        /// </summary>
        public vtkChartBar()
        {
            InitializeComponent();
        }

        protected override vtkPlot Add(vtkTableAlgorithm table, MultiControl control)
        {
            vtkPlotBar bar = vtkPlotBar.New();
            Collector collector = _collectors[control];

            collector.Variables.Plot = bar;

            bar.SetInputData(table.GetOutput(), 1, 2);
            
            return bar;
        }
    }
}
