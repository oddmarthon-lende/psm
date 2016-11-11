using System.Windows.Media;

namespace PSM.Viewer.Visualizations
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
