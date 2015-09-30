using Kitware.VTK;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;

namespace PSMViewer.Visualizations
{

    [Visible(true)]
    [DisplayName("Line (VTK)")]
    [Icon("../icons/chart_line.png")]
    public sealed partial class vtkLine : vtkChartBase
    {


        /// <summary>
        /// Sets/gets the line width
        /// </summary>
        public float LineWidth
        {
            get { return (float)GetValue(LineWidthProperty); }
            set { SetValue(LineWidthProperty, value); }
        }  
           
        /// <summary>
        /// Identifies the <see cref="LineWidth"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LineWidthProperty =
            DependencyProperty.Register("LineWidth", typeof(float), typeof(vtkLine), new PropertyMetadata(1F));



        /// <summary>
        /// Constructor
        /// </summary>
        public vtkLine()
        {            
            
            InitializeComponent();

            #region Forward Properties to VTK

            // Forwards the linewidth property to the VTK API when the value changes 
            DependencyPropertyDescriptor.FromProperty(LineWidthProperty, typeof(vtkLine)).AddValueChanged(this, delegate {

                foreach(Collector collector in _collectors.Values)
                {
                    collector.Variables.Plot.SetWidth(LineWidth);
                }

                _view.Render();

            });            

            #endregion

        }

        /// <summary>
        /// <see cref="VisualizationControl.Refresh"/>
        /// </summary>
        public override void Refresh()
        {

            foreach (Collector collector in _collectors.Values)
            {
                Color c = GetDefaultColor().Color;
                collector.Variables.Plot.GetPen().SetColor(c.R, c.G, c.B);
            }

            base.Refresh();
        }

        protected override vtkPlot Add(vtkTableAlgorithm table, MultiControl control)
        {

            vtkPlotLine line = vtkPlotLine.New();
            Collector collector = _collectors[control];

            collector.Variables.Plot = line;
                                    
            line.SetInputData(table.GetOutput(), 1, 2);
                        
            Color c = GetDefaultColor().Color;

            line.GetPen().SetColor(c.R, c.G, c.B);
            line.SetWidth(LineWidth);

            return line;
        }

    }
}
