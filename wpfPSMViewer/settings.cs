using PSMonitor.Stores;
using PSMViewer.Editors;
using PSMViewer.ViewModels;
using PSMViewer.Visualizations;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace PSMViewer
{

    /// <summary>
    /// Mainwindow options
    /// </summary>
    [Serializable]
    public class Settings : DependencyObject, INotifyPropertyChanged
    {
        /// <summary>
        /// <see cref="INotifyPropertyChanged.PropertyChanged"/>
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Triggers the <see cref="INotifyPropertyChanged.PropertyChanged"/> event
        /// </summary>
        /// <param name="propertyName"></param>
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
        
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
        Window _window;

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

            get {

                string value = (string)GetValue(IndexFieldProperty);

                try {                
                    
                    Enum.Parse(_index, value);
                }
                catch(Exception)
                {
                    value = PSMonitor.PSM.Store(Dispatcher).Default.ToString();
                }
                                
                return value;

            }

            set {

                try {

                    Enum.Parse(_index, value);                                        
                }
                catch(Exception) {
                    value = PSMonitor.PSM.Store(Dispatcher).Default.ToString();
                }
                finally
                {
                    SetValue(IndexFieldProperty, value);
                }

                OnPropertyChanged();

            }
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
                OnPropertyChanged();
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
                OnPropertyChanged();
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
        public Settings(Window window)
        {            
            _window = window;
            IndexField = PSMonitor.PSM.Store(_window.Dispatcher).Default.ToString();
        }

    }
}
