using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using PSMViewer.Models;
using PSMViewer.ViewModels;
using System;

namespace PSMViewer.Visualizations
{

    public interface IReplicator
    {
        object DataContext { get; set; }
        void Update(object value);
        void Refresh();
    }

    [Visible(false)]
    public class Replicator<T> : VisualizationControl
    {

        public Orientation Orientation
        {
            get { return (Orientation)GetValue(OrientationProperty); }
            set { SetValue(OrientationProperty, value); }
        }
        public static readonly DependencyProperty OrientationProperty =
            DependencyProperty.Register("Orientation", typeof(Orientation), typeof(Gauge), new PropertyMetadata(Orientation.Vertical));

        public double MaxColumnWidth
        {
            get
            {
                return Orientation == Orientation.Horizontal ? (ActualWidth / Controls.Count) : double.PositiveInfinity;
            }
        }

        public double MaxColumnHeight
        {
            get
            {
                return Orientation == Orientation.Vertical ? (ActualHeight / Controls.Count) : double.PositiveInfinity;
            }
        }

        private ItemsControl _items;

        private DataTemplate _template;

        public Replicator()
        {
            
            FrameworkElementFactory factory = new FrameworkElementFactory(typeof(T));
            
            factory.SetBinding(FrameworkElement.DataContextProperty, new Binding("DataContext")
            {
                RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent)
            });

            _template = new DataTemplate()
            {
                VisualTree = factory
            };

            Content = _items = new ItemsControl()
            {
                ItemsSource = Controls,
                ItemTemplate = _template,
                ItemsPanel = new ItemsPanelTemplate(new FrameworkElementFactory(typeof(UniformGrid)))
            };

            Properties.Add(new Xceed.Wpf.Toolkit.PropertyGrid.PropertyDefinition()
            {
                TargetProperties = new string[] { "Padding" }
            });

        }

        public override void Refresh()
        {

            base.Refresh();

            foreach (IReplicator g in this.Find<IReplicator>(_items))
            {
                
                try
                {

                    Control ctrl = g as Control;

                    ctrl.Background = Background;
                    ctrl.Foreground = Foreground;
                    ctrl.Padding = Padding;

                }
                catch(Exception) { }
                finally
                {
                    g.Refresh();
                }
            }
        }

        
    }
}
