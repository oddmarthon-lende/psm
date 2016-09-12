using System.Windows.Data;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System;

namespace PSMViewer.Visualizations
{

    [Visible(true)]
    [DisplayName("Log View")]
    [Icon("../icons/table.png")]
    [SubCategory("Built-In")]
    public sealed partial class LogView : TableBase
    {
                
        public LogView()
        {
            InitializeComponent();
        }
        
    }
}
