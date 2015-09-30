/// <copyright file="properties.xaml.cs" company="Baker Hughes Incorporated">
/// Copyright (c) 2015 All Rights Reserved
/// </copyright>
/// <author>Odd Marthon Lende</author>
/// <summary>Code behind for the Properties Window</summary>
/// 
using System.Linq;
using System.Windows;
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
        /// Holds the property definitions that will be visible to the user
        /// </summary>
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

                foreach (PropertyDefinition d in value)
                {
                    PropertyGrid.PropertyDefinitions.Add(d);
                }

            }

        }

        /// <summary>
        /// Gets a refrerence to the propertygrid
        /// </summary>
        public PropertyGrid PropertyGrid
        {
            get
            {
                return (PropertyGrid)Content;
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
        /// <param name="SelectedObject">The object to show properties for</param>
        /// <param name="Definitions">The properties that will be visible to the user.</param>
        public PropertiesWindow(object SelectedObject, PropertyDefinition[] Definitions)
        {

            Content = new PropertyGrid() {

                AutoGenerateProperties = false,
                ShowDescriptionByTooltip = true,
                ShowSearchBox = false,
                ShowSortOptions = false,
                ShowPreview = false,
                ShowSummary = false,
                ShowTitle = false,
                ShowAdvancedOptions = false
            };

            PropertyDefinitions = Definitions;
            PropertyGrid.SelectedObject = SelectedObject;

        }
    }
}
