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
using System.Windows.Shapes;
using Xceed.Wpf.Toolkit.PropertyGrid;

namespace PSMViewer
{

    public interface IPropertyProvider
    {
        PropertyDefinition[] Properties { get; }
    }

    public partial class PropertiesWindow : Window
    {
        
        public PropertyDefinition[] PropertyDefinitions
        {
            get
            {
                return Element.PropertyDefinitions.ToArray();
            }
            private set
            {
                if (value == null) return;

                Element.PropertyDefinitions.Clear();

                foreach(PropertyDefinition d in value)
                {
                    Element.PropertyDefinitions.Add(d);   
                }

            }

        }

        public PropertyGrid Element
        {
            get
            {
                return propertyGrid;
            }
        }
        
        public PropertiesWindow(IPropertyProvider provider) : this((object)provider, provider.Properties)
        {
            
        }

        public PropertiesWindow(object SelectedObject, PropertyDefinition[] Definitions)
        {
            InitializeComponent();

            this.Loaded += delegate
            {
                Element.SelectedObject = SelectedObject;
                PropertyDefinitions    = Definitions;
            };           

        }
    }
}
