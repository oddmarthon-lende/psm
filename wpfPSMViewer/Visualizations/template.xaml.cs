using System.Windows.Data;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System;

namespace PSMViewer.Visualizations
{

    [Visible(true)]
    [DisplayName("Name of widget")]
    [Icon("Path to icon image file")]
    [SubCategory("Subcategory (In the menu)")]
    public sealed partial class Template : VisualizationControl
    {
                
        public Template()
        {
            
            InitializeComponent();
        }
        
    }
}
