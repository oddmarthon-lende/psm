using Kitware.VTK;
using PSMViewer.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Dynamic;
using System.Windows.Data;
using System.Windows.Forms.Integration;
using System.Windows.Media;
using System.Collections.ObjectModel;
using PSMonitor;
using System.Windows;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows.Threading;
using PSMViewer.ViewModels;

namespace PSMViewer.Visualizations
{

    public class vtkSolidColorBrushList : List<SolidColorBrush> {

        public vtkSolidColorBrushList() : base()
        {

        }

        public vtkSolidColorBrushList(IEnumerable<SolidColorBrush> list) : base(list)
        {

        }

    }

    [Visible(false)]
    public class vtkBase : VisualizationControl
    {

        
        protected RenderWindowControl _renderControl = new RenderWindowControl();

        protected vtkRenderWindow _renderWindow { get { return _renderControl.RenderWindow;  } }

        protected vtkRenderer _renderer { get { return _renderWindow == null ? null : _renderWindow.GetRenderers().GetFirstRenderer();  } }

        protected vtkPolyData _source = vtkPolyData.New();

        private int _currentColorIndex = 0;

        #region Dependency Properties


        /// <summary>
        /// A list of some default colors
        /// </summary>
        public vtkSolidColorBrushList Colors
        {
            get { return (vtkSolidColorBrushList)GetValue(ColorsProperty); }
            set { SetValue(ColorsProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="Colors"/> dependency property
        /// </summary>
        public static readonly DependencyProperty ColorsProperty =
            DependencyProperty.Register("Colors", typeof(vtkSolidColorBrushList), typeof(vtkBase), new PropertyMetadata(null));



        #endregion

        public vtkBase()
        {

            if (!Debugger.IsAttached)
                vtkOutputWindow.GlobalWarningDisplayOff();

            // Add some default colors
            Colors = new vtkSolidColorBrushList(typeof(System.Windows.Media.Colors).GetProperties().ToList().Select((p) => { return new SolidColorBrush((Color)p.GetValue(null)); }));                 

            // Reset the color index when colors list is changed
            DependencyPropertyDescriptor.FromProperty(ColorsProperty, typeof(vtkBase)).AddValueChanged(this, (sender, e) => ResetColorIndex());

            // Refresh when the colors list is changed
            DependencyPropertyDescriptor.FromProperty(ColorsProperty, typeof(vtkBase)).AddValueChanged(this, (sender, e) => Refresh());
            
            // Sets the VTK renderer background color to the background property's color when it is changed
            DependencyPropertyDescriptor.FromProperty(BackgroundProperty, typeof(VisualizationControl)).AddValueChanged(this, delegate
            {

                Color color = ((SolidColorBrush)Background).Color;

                _renderer.SetBackground(color.R / 255, color.G / 255, color.B / 255);
                _renderControl.BackColor = System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);

                Refresh();

            });
            
            // Listen for collection changed events
            Controls.CollectionChanged += Controls_CollectionChanged;
            
            // Set the content to the render control
            Content = new WindowsFormsHost() { Child = _renderControl, Background = Brushes.Transparent };

            // Set the background to transparent
            _renderControl.BackColor = System.Drawing.Color.Firebrick;

            _renderControl.Load += delegate
            {
                _renderWindow.SetAAFrames(4);
                _renderWindow.SetMultiSamples(8);
                
            };

        }

        /// <summary>
        /// Resets the color index
        /// </summary>
        protected void ResetColorIndex()
        {
            _currentColorIndex = 0;
        }
                
        /// <summary>
        /// Gets a default brush
        /// </summary>
        /// <returns>A brush from the <see cref="Colors"/> list</returns>
        protected SolidColorBrush GetDefaultColor()
        {
            return Colors[GetDefaultColorIndex()];
        }

        /// <summary>
        /// Gets the current color index.
        /// </summary>
        /// <returns></returns>
        protected int GetDefaultColorIndex()
        {
            int index = _currentColorIndex;
            _currentColorIndex = ++_currentColorIndex % Colors.Count;
            return index;
        }
        
        /// <summary>
        /// Cleans up any resources created
        /// </summary>
        public override void Dispose()
        {
            Controls.CollectionChanged -= Controls_CollectionChanged;
            base.Dispose();
        }

        /// <summary>
        /// <see cref="VisualizationControl.Refresh"/>
        /// </summary>
        public override void Refresh()
        {

            if(_renderWindow != null)
                _renderWindow.Render();

            base.Refresh();
        }

        /// <summary>
        /// Monitors the <see cref="VisualizationControl.Controls"/> collection and modifies VTK objects as needed.
        /// </summary>
        protected virtual void Controls_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            
            vtkFieldData fd = _source.GetFieldData();

            switch (e.Action)
            {

                case NotifyCollectionChangedAction.Reset:

                    fd.Reset();
                    _source.Modified();

                    break;
            }            
            
        }

        /// <summary>
        /// <see cref="VisualizationControl.Remove(KeyItem)"/>
        /// </summary>
        /// <param name="key"></param>
        public override void Remove(KeyItem key)
        {
            vtkFieldData fd = _source.GetFieldData();
            MultiControl m = GetControlsFor(key);

            if(m != null)
            {
                string path = m.Key.Path;

                fd.RemoveArray(path);
                _collectors.Remove(m);

            }

            base.Remove(key);
        }

        /// <summary>
        /// <see cref="VisualizationControl.Add(KeyItem, ObservableCollection{EntryItem})"/>
        /// </summary>
        public override MultiControl Add(KeyItem key, ObservableCollection<EntryItem> collection = null)
        {

            vtkFieldData fd = _source.GetFieldData();
            MultiControl m = base.Add(key, collection);

            if(m != null)
            {
                string path = m.Key.Path;
                vtkDoubleArray array = vtkDoubleArray.New();

                array.SetName(path);
                array.SetNumberOfComponents(3);
                array.SetComponentName(0, "Value");
                array.SetComponentName(1, "Index");

                fd.AddArray(array);

                Collector collector = new Collector(this, m.Key);

                _collectors.Add(m, collector);

                m.Entries.CollectionChanged += collector.Handler;
                collector.Updated += Collector_Updated;

            }

            return m;
        }

        private DispatcherOperation _modifiedOperation = null;

        protected Queue<Collector> _dirty = new Queue<Collector>();

        protected virtual void UpdateView()
        {
            _modifiedOperation = null;                        
        }

        protected virtual void OnModified()
        {
            if (_modifiedOperation == null)
                _modifiedOperation = Dispatcher.InvokeAsync(UpdateView, DispatcherPriority.ContextIdle);
            else
            {

            }
        }

        protected void Collector_Updated(Collector obj)
        {
            if (!_dirty.Contains(obj))
                _dirty.Enqueue(obj);

            OnModified();

        }

        protected Dictionary<MultiControl, Collector> _collectors = new Dictionary<MultiControl, Collector>();

        /// <summary>
        /// This class collects data and modifies the <see cref="_source"/> as needed.
        /// </summary>
        protected class Collector
        {

            /// <summary>
            /// This event is emitted when data has been updated\changed.
            /// </summary>
            public event Action<Collector> Updated;

            /// <summary>
            /// A dynamic object that can be expanded with custom fields.
            /// </summary>
            public dynamic Variables { get; private set; } = new ExpandoObject();

            /// <summary>
            /// The key associated with this instance
            /// </summary>
            private KeyItem _key;

            /// <summary>
            /// The parent instance
            /// </summary>
            private vtkBase _instance;
                       
            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="instance">A reference to the parent instance</param>
            /// <param name="key">The key assosiated with this collector</param>
            public Collector(vtkBase instance, KeyItem key)
            {
                this._key = key;
                this._instance = instance;
                
            }

            /// <summary>
            /// Called when the data has been updated/changed.
            /// </summary>
            protected void OnUpdate()
            {
                _instance._source.Modified();

                if (Updated != null)
                    Updated(this);
            }

            /// <summary>
            /// The handler listens to <see cref="ObservableCollection{VisualizationControl.MultiControl}.CollectionChanged"/> event.
            /// </summary>
            public void Handler (object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
            {

                vtkDoubleArray _array = (vtkDoubleArray)_instance._source.GetFieldData().GetArray(_key.Path);

                if (_array == null) return;
                
                switch (e.Action)
                {

                    case NotifyCollectionChangedAction.Reset:

                        string name = _array.GetName();
                        vtkFieldData fd = _instance._source.GetFieldData();
                        
                        fd.RemoveArray(name);

                        _array = vtkDoubleArray.New();
                        _array.SetName(name);
                        _array.SetNumberOfComponents(3);

                        _array.SetComponentName(0, "Value");
                        _array.SetComponentName(1, "Index");

                        fd.AddArray(_array);

                        break;

                    case NotifyCollectionChangedAction.Add:

                        foreach (EntryItem entry in e.NewItems)
                        {
                            _array.InsertNextTuple3(_key.Units.Convert<double>((Entry)entry), Convert.ToDouble(entry.Index), 0);
                        }

                        break;
                }               

                OnUpdate();

            }

        }
    }
}
