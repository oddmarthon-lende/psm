using PSMViewer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PSMViewer.Visualizations
{
   
    public sealed partial class Text : VisualizationControl
    {

        public static string DisplayName { get { return typeof(Text).Name; } }
        public static string Icon { get { return @"..\icons\text_columns.png";  } }
        
        public Text()
        {
            InitializeComponent();           
        }
    }
}
