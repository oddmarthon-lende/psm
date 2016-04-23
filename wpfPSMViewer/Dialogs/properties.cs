using System.ComponentModel;
/// <copyright file="properties.xaml.cs" company="Baker Hughes Incorporated">
/// Copyright (c) 2015 All Rights Reserved
/// </copyright>
/// <author>Odd Marthon Lende</author>
/// <summary>Code behind for the Properties Window</summary>
/// 
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;
using Xceed.Wpf.Toolkit.PropertyGrid;

namespace PSMViewer
{

    /// <summary>
    /// The interface
    /// </summary>
    public interface IPropertyProvider
    {
        PropertyDefinition[] Properties { get; }
    }

    /// <summary>
    /// A simple properties window that display a property grid
    /// </summary>
    public partial class PropertiesWindow : Window
    {
        
        /// <summary>
        /// Gets a refrerence to the propertygrid
        /// </summary>
        public PropertyGrid[] PropertyGrids
        {
            get
            {

                if (Content is PropertyGrid)
                    return new PropertyGrid[] { (PropertyGrid)Content };
                
                return this.Find<PropertyGrid>(this);

            }
        }

        /// <summary>
        /// A constructor that takes an <see cref="IPropertyProvider"/> as an argument
        /// </summary>
        /// <param name="provider"></param>
        public PropertiesWindow(IPropertyProvider provider) : this((object)provider, provider.Properties) { }

        /// <summary>
        /// A constructor that takes the object and property definitions as its arguments.
        /// </summary>
        /// <param name="selectedObject">The object to show properties for</param>
        /// <param name="definitions">The properties that will be visible to the user.</param>
        public PropertiesWindow(object selectedObject, PropertyDefinition[] definitions = null)
        {

            PropertyGrid g = new PropertyGrid()
            {

                AutoGenerateProperties = definitions == null,
                ShowDescriptionByTooltip = true,
                ShowSearchBox = false,
                ShowSortOptions = false,
                ShowPreview = false,
                ShowSummary = false,
                ShowTitle = false,
                ShowAdvancedOptions = false
            };           

            if (definitions != null)
                foreach (PropertyDefinition d in definitions)
                {
                    g.PropertyDefinitions.Add(d);
                }

            g.SelectedObject = selectedObject;

            Content = g;

            AddHandler(selectedObject, g);

        }

        private void AddHandler(object selectedObject, PropertyGrid grid)
        {
            if(selectedObject is INotifyPropertyChanged)
            {
                ((INotifyPropertyChanged)selectedObject).PropertyChanged += (sender, e) => grid.Update();
            }
        }

        public PropertiesWindow(params object[] selectedObjects)
        {

            Content = new StackPanel() { Orientation = Orientation.Vertical };

            foreach (object p in selectedObjects)
            {

                PropertyGrid g = new PropertyGrid()
                {

                    AutoGenerateProperties = !(p is IPropertyProvider),
                    ShowDescriptionByTooltip = true,
                    ShowSearchBox = false,
                    ShowSortOptions = false,
                    ShowPreview = false,
                    ShowSummary = false,
                    ShowTitle = false,
                    ShowAdvancedOptions = false

                };

                if (p is IPropertyProvider)
                {

                    foreach(PropertyDefinition d in ((IPropertyProvider)p).Properties)
                    {
                        g.PropertyDefinitions.Add(d);
                    }

                }                    

                ((StackPanel)Content).Children.Add(g);

                g.SelectedObject = p;

                AddHandler(p, g);

            }
        }
    }
}
