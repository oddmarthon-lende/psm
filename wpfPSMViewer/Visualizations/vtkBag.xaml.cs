using Kitware.VTK;
using PSMViewer.ViewModels;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;

namespace PSMViewer.Visualizations
{

    [Visible(true)]
    [DisplayName("Bag (VTK)")]
    [Icon("../icons/image.png")]
    public sealed partial class vtkBag : vtkChartBase
    {
        
        /// <summary>
        /// Constructor
        /// </summary>
        public vtkBag()
        {            
            
            InitializeComponent();            

        }

        protected override vtkPlot Add(vtkTableAlgorithm algorithm, MultiControl control)
        {
            Debug.WriteLine(algorithm.GetOutput());
            vtkPlotBag bag = vtkPlotBag.New();
            vtkHighestDensityRegionsStatistics hrds = vtkHighestDensityRegionsStatistics.New();

            Collector collector = _collectors[control];

            collector.Variables.Plot = bag;

            hrds.SetInputConnection(algorithm.GetOutputPort());

            hrds.AddColumnPair("Index", control.Key.Name);
            hrds.SetLearnOption(true);
            hrds.SetDeriveOption(true);
            hrds.SetAssessOption(false);
            hrds.SetTestOption(false);

            hrds.Update();

            vtkMultiBlockDataSet mbd = (vtkMultiBlockDataSet)hrds.GetOutputDataObject((int)vtkStatisticsAlgorithm.OutputIndices.OUTPUT_MODEL);

            vtkTable table = (vtkTable)mbd.GetBlock(0);

            Debug.WriteLine(table);

            bag.SetInputData(algorithm.GetOutput(), 0, 1, 2);                                    

            return bag;
        }

    }
}
