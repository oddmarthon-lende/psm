/// <copyright file="replicator.cs" company="Baker Hughes Incorporated">
/// Copyright (c) 2015 All Rights Reserved
/// </copyright>
/// <author>Odd Marthon Lende</author>
/// <summary>Replicate visualization controls</summary>
/// 
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System;
using System.ComponentModel;
using PSM.Viewer.Models;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using Xceed.Wpf.Toolkit.PropertyGrid;
using System.IO;
using System.Windows.Markup;
using PSM.Viewer.Commands;
using System.Collections.ObjectModel;

namespace PSM.Viewer.Visualizations
{
    

    /// <summary>
    /// 
    /// </summary>
    [DisplayName("Replicator")]
    public class Replicator : VisualizationControl
    {

        /// <summary>
        /// 
        /// </summary>
        public enum ReplicatorGroupBy
        {
            PathComponent,
            Wildcard,
            Path
        }

        /// <summary>
        /// 
        /// </summary>
        public enum ReplicatorPanelType
        {
            Stacked,
            Uniform

        }

        /// <summary>
        /// A collection that caches replicated widgets
        /// </summary>
        public class ChildrenCollection : Dictionary<string, VisualizationControl>
        {
            public new void Clear()
            {
                
                foreach (VisualizationControl v in this.Values)
                {
                    foreach (MultiControl ctrl in v.Controls.ToArray())
                        v.Remove(ctrl.Key);

                    v.Dispose();
                }

                base.Clear();
            }
        }

        /// <summary>
        /// Removes <see cref="Replicator"/> from the list
        /// </summary>
        public class WidgetTypeEditor : Editors.WidgetTypeEditor {

            protected override IEnumerable CreateItemsSource(PropertyItem propertyItem)
            {
                List<InheritorInfo> items = new List<InheritorInfo>();

                foreach(InheritorInfo info in base.CreateItemsSource(propertyItem))
                {
                    if (info.Type == typeof(Replicator))
                        continue;

                    items.Add(info);
                }

                return items;
            }

        }

        /// <summary>
        /// Backing field for the <see cref="Base"/> property
        /// </summary>
        private VisualizationControl _base;
        /// <summary>
        /// 
        /// </summary>
        public virtual VisualizationControl Base
        {
            get
            {
                if (WidgetType != null && _base == null)
                    SetField(ref _base, (VisualizationControl)WidgetType.New());

                return _base;
            }

            set
            {
                WidgetType = value.GetType();
                SetField(ref _base, value);

                foreach(Xceed.Wpf.Toolkit.PropertyGrid.PropertyDefinition def in _base.Properties.ToArray())
                {
                    if (def.Category == "Controls")
                        _base.Properties.Remove(def);
                }
            }
        }

        /// <summary>
        /// The type of <see cref="VisualizationControl"/> to replicate
        /// </summary>
        [Editor(typeof(WidgetTypeEditor), typeof(WidgetTypeEditor))]
        public Type WidgetType
        {
            get { return (Type)GetValue(WidgetTypeProperty); }
            set { SetValue(WidgetTypeProperty, value); }
        }
        public static readonly DependencyProperty WidgetTypeProperty =
            DependencyProperty.Register("WidgetType", typeof(Type), typeof(Replicator), new FrameworkPropertyMetadata(null, (sender, e) =>
            {
                Replicator r = sender as Replicator;

                if (e.NewValue.GetType() != r.Base.GetType())
                {
                    r.Base = (VisualizationControl)((Type)e.NewValue).New();
                    r._children.Clear();
                }

            }));


        /// <summary>
        /// The layout panel type used to in the layout of replicated controls
        /// </summary>
        public ReplicatorPanelType PanelType
        {
            get { return (ReplicatorPanelType)GetValue(PanelTypeProperty); }
            set { SetValue(PanelTypeProperty, value); }
        }
        public static readonly DependencyProperty PanelTypeProperty =
            DependencyProperty.Register("PanelType", typeof(ReplicatorPanelType), typeof(Replicator), new FrameworkPropertyMetadata(ReplicatorPanelType.Stacked, (sender, e) =>
            {

                Replicator r = sender as Replicator;
                r.SetPanel((ReplicatorPanelType)e.NewValue);

            }));


        /// <summary>
        /// How to group the metrics
        /// </summary>
        public ReplicatorGroupBy GroupBy
        {
            get { return (ReplicatorGroupBy)GetValue(GroupByProperty); }
            set { SetValue(GroupByProperty, value); }
        }
        public static readonly DependencyProperty GroupByProperty =
            DependencyProperty.Register("GroupBy", typeof(ReplicatorGroupBy), typeof(Replicator), new PropertyMetadata(ReplicatorGroupBy.Path, (sender, e) =>
            {
                Replicator r = sender as Replicator;
                r._children.Clear();
            }));

        /// <summary>
        /// Number of columns when <see cref="PanelType"/> is <see cref="ReplicatorPanelType.Uniform"/>
        /// </summary>
        public int Columns
        {
            get { return (int)GetValue(ColumnsProperty); }
            set { SetValue(ColumnsProperty, value); }
        }
        public static readonly DependencyProperty ColumnsProperty =
            DependencyProperty.Register("Columns", typeof(int), typeof(Replicator), new PropertyMetadata(0));


        /// <summary>
        /// Number of rows when <see cref="PanelType"/> is <see cref="ReplicatorPanelType.Uniform"/>
        /// </summary>
        public int Rows
        {
            get { return (int)GetValue(RowsProperty); }
            set { SetValue(RowsProperty, value); }
        }
        public static readonly DependencyProperty RowsProperty =
            DependencyProperty.Register("Rows", typeof(int), typeof(Replicator), new PropertyMetadata(0));

        /// <summary>
        /// The orientation used when <see cref="PanelType"/> is <see cref="ReplicatorPanelType.Stacked"/>
        /// </summary>
        public Orientation Orientation
        {
            get { return (Orientation)GetValue(OrientationProperty); }
            set { SetValue(OrientationProperty, value); }
        }
        public static readonly DependencyProperty OrientationProperty =
            DependencyProperty.Register("Orientation", typeof(Orientation), typeof(Replicator), new PropertyMetadata(Orientation.Vertical));

        private uint _position = 0;
        /// <summary>
        /// The component position when <see cref="GroupBy"/> is <see cref="ReplicatorGroupBy.PathComponent"/>
        /// </summary>
        public uint Position
        {
            get
            {
                return _position;
            }

            set
            {
                if (SetField(ref _position, value))
                    _children.Clear();
            }
        }

        private bool _canContentScroll = false;
        public bool CanContentScroll
        {
            get
            {
                return _canContentScroll;
            }

            set
            {
                SetField(ref _canContentScroll, value);

                if (_canContentScroll)
                    Content = (_canContentScroll ? (object)(new ScrollViewer() { Content = _items }) : _items);
            }

        }

        private ChildrenCollection _children = new ChildrenCollection();

        /// <summary>
        /// Gets the children
        /// </summary>
        public IEnumerable<VisualizationControl> Children
        {

            get
            {

                if (WidgetType == null)
                    return null;

                string xaml;

                using(MemoryStream stream = new MemoryStream())
                {

                    Base.Export(stream);

                    stream.Seek(0, SeekOrigin.Begin);

                    using (StreamReader reader = new StreamReader(stream))
                    {
                        xaml = reader.ReadToEnd();
                    }

                }

                return Controls.GroupBy((m) =>
                {

                    switch (GroupBy)
                    {
                        case ReplicatorGroupBy.Path:

                            return m.Key.Path;

                        case ReplicatorGroupBy.PathComponent:

                            string[] components = Path.Extract(m.Key.Path).Components.ToArray();
                            return Position > components.Length - 1 ? "" : components[Position];

                        case ReplicatorGroupBy.Wildcard:

                            return m.Key.W == null ? "" : m.Key.W.Path;
                    }

                    return null;

                }, (m) => m).Select((group) =>
                {

                    VisualizationControl widget = _children.ContainsKey(group.Key) ? _children[group.Key] : (VisualizationControl)XamlReader.Parse(xaml);

                    widget.Title = group.Key;

                    if (_children.ContainsKey(group.Key))
                        return widget;

                    foreach (MultiControl c in group)
                    {
                        widget.Add(c.Key, c.Entries);
                        this.OnReload(c);
                    }

                    if (!_children.ContainsKey(group.Key))
                        _children.Add(group.Key, widget);

                    return widget;

                });
            }
        }


        /// <summary>
        /// References the <see cref="ItemsControl"/> used to display the children
        /// </summary>
        private ItemsControl _items;
        
        /// <summary>
        /// References the panel template
        /// </summary>
        private ItemsPanelTemplate _panel;

        /// <summary>
        /// Sets the panel used by the <see cref="ItemsControl"/> to layout the children
        /// </summary>
        /// <param name="p"></param>
        private void SetPanel(ReplicatorPanelType p)
        {

            FrameworkElementFactory factory = null;

            switch (p)
            {
                case ReplicatorPanelType.Stacked:

                    factory = new FrameworkElementFactory(typeof(StackPanel));

                    factory.SetBinding(StackPanel.OrientationProperty, new Binding("Orientation")
                    {
                        Source = this,
                        Mode = BindingMode.OneWay
                    });

                    break;

                case ReplicatorPanelType.Uniform:

                    factory = new FrameworkElementFactory(typeof(UniformGrid));

                    factory.SetBinding(UniformGrid.ColumnsProperty, new Binding("Columns")
                    {
                        Source = this,
                        Mode = BindingMode.OneWay
                    });

                    factory.SetBinding(UniformGrid.RowsProperty, new Binding("Rows")
                    {
                        Source = this,
                        Mode = BindingMode.OneWay
                    });

                    break;
            }

            _panel = new ItemsPanelTemplate(factory);

            if (_items != null)
                _items.ItemsPanel = _panel;

        }
        /// <summary>
        /// Constructor
        /// </summary>
        public Replicator()
        {

            SetPanel(ReplicatorPanelType.Stacked);

            _items = new ItemsControl()
            {
                ItemsPanel = _panel
            };

            _items.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Auto);
            _items.SetValue(ScrollViewer.VerticalScrollBarVisibilityProperty, ScrollBarVisibility.Auto);

            Content = _items;

            _items.SetBinding(ItemsControl.ItemsSourceProperty, new Binding("Children")
            {
                Source = this
            });

            #region Properties    

                Properties.Add(new Xceed.Wpf.Toolkit.PropertyGrid.PropertyDefinition()
                {
                    Category = "Replicator",
                    TargetProperties = new string[] { "PanelType", "WidgetType", "GroupBy", "Position", "CanContentScroll"}
                });

                Properties.Add(new Xceed.Wpf.Toolkit.PropertyGrid.PropertyDefinition()
                {
                    Category = "Replicator.Stacked",
                    TargetProperties = new string[] { "Orientation" }
                });

                Properties.Add(new Xceed.Wpf.Toolkit.PropertyGrid.PropertyDefinition()
                {
                    Category = "Replicator.Uniform",
                    TargetProperties = new string[] { "Columns", "Rows"}
                });

            #endregion

            RegisterUserCommand("Edit Template", new RelayCommand((a, b) =>
            {
                
                Base.Owner = this.Owner;
                Base.Commands.First((pair) =>
                {
                    if (pair.Key == "Properties")
                        return true;

                    return false;
                }).Value.Execute();
                _children.Clear();
                Refresh();
                this.OnReload(this);
            }, (a, b) => 
            {
                return Base != null;
            }));


        }

        public override bool Add(KeyItem key, ObservableCollection<Entry> collection = null)
        {
            _children.Clear();
            return base.Add(key, collection);
        }

        /// <summary>
        /// <see cref="VisualizationControl.Refresh"/>
        /// </summary>
        public override void Refresh()
        {
           
            _items.UpdateBindingTargets(ItemsControl.ItemsSourceProperty);
            base.Refresh();

        }

        
    }
}
