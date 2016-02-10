using PSMonitor.Stores;
using PSMViewer.Editors;
using PSMViewer.ViewModels;
using PSMViewer.Visualizations;
using System;
using System.ComponentModel;
using System.Windows;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace PSMViewer
{

    /// <summary>
    /// Mainwindow options
    /// </summary>
    [Serializable]
    public class Settings : DependencyObject
    {

        /// <summary>
        /// A list of chart types for the <see cref="ChartType"/> property.
        /// </summary>
        public sealed class ChartTypes : ItemCollection, IItemsSource
        {
            public ItemCollection GetValues()
            {
                return this;
            }

            public ChartTypes()
            {
                foreach(var info in VisualizationControl.List)
                {
                    Add(info.Type, info.DisplayName);
                }
                
            }
        }

        /// <summary>
        /// References the main window.
        /// </summary>
        MainWindow _window = (MainWindow)App.Current.MainWindow;

        /// <summary>
        /// The type of chart to display in the main window.
        /// </summary>
        [Category("Application")]
        [ItemsSource(typeof(ChartTypes))]
        [Description("Gets/Sets the type of chart to display in the main window")]
        public Type ChartType
        {
            get { return (Type)GetValue(ChartTypeProperty); }
            set { SetValue(ChartTypeProperty, value); }
        }
        /// <summary>
        /// Identifies the <see cref="ChartType"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ChartTypeProperty =
            DependencyProperty.Register("ChartType", typeof(Type), typeof(Settings), new PropertyMetadata(null));
        


        [Category("Controls")]
        [Editor(typeof(StoreEnumEditor), typeof(StoreEnumEditor))]
        [PropertyOrder(-1)]
        [Description("Gets/Sets the data index field")]
        public string IndexField
        {
            get { return (string)GetValue(IndexFieldProperty); }
            set { SetValue(IndexFieldProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="IndexField"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IndexFieldProperty =
            DependencyProperty.Register("IndexField", typeof(string), typeof(Settings), new PropertyMetadata(null));



        [Category("Controls")]
        [PropertyOrder(0)]
        [Description("Gets/Sets the start index")]
        public object StartIndex
        {
            get
            {
                return ((MultiControl)_window.DataContext).Get((Enum)Enum.Parse(_index, IndexField)).Start;
            }

            set
            {
                ((MultiControl)_window.DataContext).Get((Enum)Enum.Parse(_index, IndexField)).Start = value;
            }
        }

        [Category("Controls")]
        [PropertyOrder(1)]
        [Description("Gets/Sets the end index")]
        public object EndIndex
        {
            get
            {

                return ((MultiControl)_window.DataContext).Get((Enum)Enum.Parse(_index, IndexField)).End;
            }

            set
            {
                ((MultiControl)_window.DataContext).Get((Enum)Enum.Parse(_index, IndexField)).End = value;
            }
        }

        [Category("Store")]
        [System.ComponentModel.DisplayName("Options")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IOptions Store
        {
            get
            {
                return PSMonitor.PSM.Store(_window.Dispatcher).Options;
            }
        }

        /// <summary>
        /// Gets the store index type
        /// </summary>
        private Type _index
        {
            get
            {
                return PSMonitor.PSM.Store(_window.Dispatcher).Index;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="window">The <see cref="MainWindow"/></param>
        public Settings(MainWindow window) : this()
        {            
            _window = window;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public Settings()
        {
            IndexField = PSMonitor.PSM.Store(_window.Dispatcher).Default.ToString();
        }

    }
}
