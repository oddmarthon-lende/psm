using System.Windows.Data;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System;

namespace PSMViewer.Visualizations
{

    [Visible(true)]
    [DisplayName("Key\\Value List")]
    [Icon("../icons/table.png")]
    [SubCategory("Built-In")]
    public sealed partial class KeyValueList : TableBase
    {
                
        public KeyValueList()
        {
            InitializeComponent();
        }
        
    }
}
