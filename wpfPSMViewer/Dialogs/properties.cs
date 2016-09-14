using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
/// <copyright file="properties.xaml.cs" company="Baker Hughes Incorporated">
/// Copyright (c) 2015 All Rights Reserved
/// </copyright>
/// <author>Odd Marthon Lende</author>
/// <summary>Code behind for the Properties Window</summary>
/// 
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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
    public class PropertiesWindow : Window
    {

        public static event System.Action<PropertiesWindow> Created;

        private List<PropertyGrid> _grids = new List<PropertyGrid>();
        /// <summary>
        /// Gets a refrerence to the propertygrid
        /// </summary>
        public IEnumerable<PropertyGrid> PropertyGrids {

            get {

                return _grids;

            }

        }
        
        private ImageSource _icon = BitmapFrame.Create(Assembly.GetExecutingAssembly().GetManifestResourceStream("PSMViewer.Icons.application_form_edit.png"));


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
            Icon = _icon;

            if (Created != null)
                Created(this);

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

            _grids.Add(g);      

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

            Icon = _icon;

            if (Created != null)
                Created(this);

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

                _grids.Add(g);

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
