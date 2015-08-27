
/// <copyright file="properties.xaml.cs" company="Baker Hughes Incorporated">
/// Copyright (c) 2015 All Rights Reserved
/// </copyright>
/// <author>Odd Marthon Lende</author>
/// <summary>Code behind for the Properties Window</summary>

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
                return PropertyGrid.PropertyDefinitions.ToArray();
            }
            private set
            {
                if (value == null) return;

                PropertyGrid.PropertyDefinitions.Clear();

                foreach(PropertyDefinition d in value)
                {
                    PropertyGrid.PropertyDefinitions.Add(d);   
                }

            }

        }

        public PropertyGrid PropertyGrid
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
                PropertyGrid.SelectedObject = SelectedObject;
                PropertyDefinitions    = Definitions;
            };           

        }
    }
}
