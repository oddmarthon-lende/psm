﻿/// <copyright file="oxyBase.cs" company="Baker Hughes Incorporated">
/// Copyright (c) 2015 All Rights Reserved
/// </copyright>
/// <author>Odd Marthon Lende</author>
/// <summary>A base for all widgets using <see cref="OxyPlot"/></summary>
/// 

using System;
using System.Collections.Generic;
using System.Linq;
using PSMViewer.Models;
using System.Collections;
using System.Windows.Media;
using Xceed.Wpf.Toolkit.PropertyGrid;
using System.Windows.Data;
using PSMViewer.Converters;
using System.Windows;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using System.Windows.Controls;
using PSMonitor;
using PSMViewer.ViewModels;
using SciChart.Charting.Visuals;
using SciChart.Charting.Visuals.RenderableSeries;
using SciChart.Charting.Model.DataSeries;

namespace PSMViewer.Visualizations
{
    
    /// <summary>
    /// Base class used when creating widgets using <see cref="OxyPlot"/>
    /// </summary>
    /// <typeparam name="T">The oxyplot chart type</typeparam>
    public class SciChartBase<T> : VisualizationControl, IDisposable
    {
        
        public delegate void SeriesAddedEventHandler(MultiControl control, T series);
        public delegate void SeriesRemovedEventHandler(MultiControl control, T series);

        public event SeriesAddedEventHandler SeriesAdded;
        public event SeriesRemovedEventHandler SeriesRemoved;
        
        protected void OnSeriesRemoved(KeyItem key, MultiControl control, T series)
        {
            if (SeriesRemoved != null)
                SeriesRemoved(control, series);
        }

        protected void OnSeriesAdded(KeyItem key, MultiControl control, T series)
        {
            if (SeriesAdded != null)
                SeriesAdded(control, series);
        }

        public SciChartSurface Surface { get; private set; } = new SciChartSurface();
        
        public override void Refresh()
        {
            
            FrameworkElement Parent = (FrameworkElement)this.Parent;

            // Had to add styling for the TrackerControl like this, because i could not find a better way to set the style for the TrackerControl.
            // The Resources property is not a dependency property and is always serialized, so add it to the parent and it will propagate down the tree and style applied.
            //
            if (Parent != null)
            {

                Style TrackerControlStyle = new Style() { TargetType = typeof(OxyPlot.Wpf.TrackerControl) };

                Parent.Resources.Remove(TrackerControlStyle.TargetType);

                Setter setter = new Setter() { Property = OxyPlot.Wpf.TrackerControl.BackgroundProperty, Value = ToolTipBackground };
                TrackerControlStyle.Setters.Add(setter);

                setter = new Setter() { Property = OxyPlot.Wpf.TrackerControl.ForegroundProperty, Value = ToolTipForeground };
                TrackerControlStyle.Setters.Add(setter);

                Parent.Resources.Add(TrackerControlStyle.TargetType, TrackerControlStyle);

            }            

            Model.InvalidatePlot(false);

            foreach (KeyValuePair<KeyItem, T> s in Series)
            {
                ((Series)(object)s.Value).Title = s.Key.Title.ToString();
            }

            base.Refresh();
        }

        protected void DataChanged(object sender)
        {
            //Refresh();
            
            foreach(MultiControl c in Controls)
            {

                if (c.Entries.Count > 0)
                    SetAxis(AxisPosition.Bottom, c.Entries[0].Index.GetType());

                break;
            }

            Model.InvalidatePlot(true);
        }

        #region Dependency Properties
        
        [ExpandableObject]
        public SolidColorBrush MinorGridLineColor
        {
            get { return (SolidColorBrush)GetValue(MinorGridLineColorProperty); }
            set { SetValue(MinorGridLineColorProperty, value); }
        }
        public static readonly DependencyProperty MinorGridLineColorProperty =
            DependencyProperty.Register("MinorGridLineColor", typeof(SolidColorBrush), typeof(OxyBase<T>), new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromRgb(0, 0, 0)), FrameworkPropertyMetadataOptions.AffectsRender));
        

        public double MinorGridlineThickness
        {
            get { return (double)GetValue(MinorGridlineThicknessProperty); }
            set { SetValue(MinorGridlineThicknessProperty, value); }
        }
        public static readonly DependencyProperty MinorGridlineThicknessProperty =
            DependencyProperty.Register("MinorGridlineThickness", typeof(double), typeof(OxyBase<T>), new FrameworkPropertyMetadata(1D, FrameworkPropertyMetadataOptions.AffectsRender));


        public double MinorStep
        {
            get { return (double)GetValue(MinorStepProperty); }
            set { SetValue(MinorStepProperty, value); }
        }
        public static readonly DependencyProperty MinorStepProperty =
            DependencyProperty.Register("MinorStep", typeof(double), typeof(OxyBase<T>), new FrameworkPropertyMetadata(double.NaN, FrameworkPropertyMetadataOptions.AffectsRender));
        

        public double MinorTickSize
        {
            get { return (double)GetValue(MinorTickSizeProperty); }
            set { SetValue(MinorTickSizeProperty, value); }
        }
        public static readonly DependencyProperty MinorTickSizeProperty =
            DependencyProperty.Register("MinorTickSize", typeof(double), typeof(OxyBase<T>), new FrameworkPropertyMetadata(4D, FrameworkPropertyMetadataOptions.AffectsRender));
        

        [ExpandableObject]
        public SolidColorBrush MajorGridLineColor
        {
            get { return (SolidColorBrush)GetValue(MajorGridLineColorProperty); }
            set { SetValue(MajorGridLineColorProperty, value); }
        }
        public static readonly DependencyProperty MajorGridLineColorProperty =
            DependencyProperty.Register("MajorGridLineColor", typeof(SolidColorBrush), typeof(OxyBase<T>), new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromRgb(0, 0, 0)), FrameworkPropertyMetadataOptions.AffectsRender));
        

        public double MajorGridlineThickness
        {
            get { return (double)GetValue(MajorGridlineThicknessProperty); }
            set { SetValue(MajorGridlineThicknessProperty, value); }
        }
        public static readonly DependencyProperty MajorGridlineThicknessProperty =
            DependencyProperty.Register("MajorGridlineThickness", typeof(double), typeof(OxyBase<T>), new FrameworkPropertyMetadata(1D, FrameworkPropertyMetadataOptions.AffectsRender));


        public double MajorStep
        {
            get { return (double)GetValue(MajorStepProperty); }
            set { SetValue(MajorStepProperty, value); }
        }
        public static readonly DependencyProperty MajorStepProperty =
            DependencyProperty.Register("MajorStep", typeof(double), typeof(OxyBase<T>), new FrameworkPropertyMetadata(double.NaN, FrameworkPropertyMetadataOptions.AffectsRender));
        

        public double MajorTickSize
        {
            get { return (double)GetValue(MajorTickSizeProperty); }
            set { SetValue(MajorTickSizeProperty, value); }
        }
        public static readonly DependencyProperty MajorTickSizeProperty =
            DependencyProperty.Register("MajorTickSize", typeof(double), typeof(OxyBase<T>), new FrameworkPropertyMetadata(7D, FrameworkPropertyMetadataOptions.AffectsRender));


        [ExpandableObject]
        public SolidColorBrush LegendBackground
        {
            get { return (SolidColorBrush)GetValue(LegendBackgroundProperty); }
            set { SetValue(LegendBackgroundProperty, value); }
        }
        public static readonly DependencyProperty LegendBackgroundProperty =
            DependencyProperty.Register("LegendBackground", typeof(SolidColorBrush), typeof(OxyBase<T>), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));
        
        public double StrokeThickness
        {
            get { return (double)GetValue(StrokeThicknessProperty); }
            set { SetValue(StrokeThicknessProperty, value); }
        }

        public static readonly DependencyProperty StrokeThicknessProperty =
            DependencyProperty.Register("StrokeThickness", typeof(double), typeof(OxyBase<T>), new FrameworkPropertyMetadata(1D, FrameworkPropertyMetadataOptions.AffectsRender));

                
        public double LegendBorderThickness
        {
            get { return (double)GetValue(LegendBorderThicknessProperty); }
            set { SetValue(LegendBorderThicknessProperty, value); }
        }
        public static readonly DependencyProperty LegendBorderThicknessProperty =
            DependencyProperty.Register("LegendBorderThickness", typeof(double), typeof(OxyBase<T>), new FrameworkPropertyMetadata(0D, FrameworkPropertyMetadataOptions.AffectsRender));


        public bool IsLegendVisible
        {
            get { return Model.IsLegendVisible; }
            set { Model.IsLegendVisible = value; Model.InvalidatePlot(true); }

        }
        

        [ExpandableObject]
        public SolidColorBrush ToolTipBackground
        {
            get { return (SolidColorBrush)GetValue(ToolTipBackgroundProperty); }
            set { SetValue(ToolTipBackgroundProperty, value); }
        }public static readonly DependencyProperty ToolTipBackgroundProperty =
            DependencyProperty.Register("ToolTipBackground", typeof(SolidColorBrush), typeof(OxyBase<T>), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        [ExpandableObject]
        public SolidColorBrush ToolTipForeground
        {
            get { return (SolidColorBrush)GetValue(ToolTipForegroundProperty); }
            set { SetValue(ToolTipForegroundProperty, value); }
        }
        public static readonly DependencyProperty ToolTipForegroundProperty =
           DependencyProperty.Register("ToolTipForeground", typeof(SolidColorBrush), typeof(OxyBase<T>), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));




        public string YAxisLabel
        {
            get { return (string)GetValue(YAxisLabelProperty); }
            set { SetValue(YAxisLabelProperty, value); }
        }
        public static readonly DependencyProperty YAxisLabelProperty =
            DependencyProperty.Register("YAxisLabel", typeof(string), typeof(OxyBase<T>), new PropertyMetadata(null));



        public string XAxisLabel
        {
            get { return (string)GetValue(XAxisLabelProperty); }
            set { SetValue(XAxisLabelProperty, value); }
        }
        public static readonly DependencyProperty XAxisLabelProperty =
            DependencyProperty.Register("XAxisLabel", typeof(string), typeof(OxyBase<T>), new PropertyMetadata(null));




        /// <summary>
        /// Y-Scale maximum value
        /// </summary>
        public double Maximum
        {
            get { return (double)GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }
        /// <summary>
        /// Identifies the <see cref="Maximum"/> dependency property
        /// </summary>
        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register("Maximum", typeof(double), typeof(OxyBase<T>), new PropertyMetadata(double.NaN));


        /// <summary>
        /// The Y-Scale minimum value
        /// </summary>
        public double Minimum
        {
            get { return (double)GetValue(MinimumProperty); }
            set { SetValue(MinimumProperty, value); }
        }
        /// <summary>
        /// Identifies the <see cref="Minimum"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register("Minimum", typeof(double), typeof(OxyBase<T>), new PropertyMetadata(double.NaN));

        #endregion


        //protected void RemoveAxis(AxisPosition pos)
        //{

        //    foreach(Axis axis in Model.Axes.ToArray())
        //    {
        //        if (axis.Position == pos)
        //        {
        //            Model.Axes.Remove(axis);
        //        }                    
        //    }
        //}

        //protected Axis GetAxis(AxisPosition pos)
        //{
        //    foreach (Axis axis in Model.Axes)
        //    {
        //        if (axis.Position == pos)
        //            return axis;
        //    }

        //    return null;
        //}

        //protected virtual void SetAxis(AxisPosition pos, Type type = null)
        //{

        //    Axis axis = GetAxis(pos);

        //    switch(type == null ? "" : type.Name)
        //    {

        //        case "DateTime":

        //            if(axis == null || !axis.GetType().Equals(typeof(DateTimeAxis)))
        //            {
        //                RemoveAxis(pos);
        //                Model.Axes.Add(new DateTimeAxis { Position = pos, Angle = 45 });
        //            }
        //            break;

        //        default:

        //            if(axis == null || !axis.GetType().Equals(typeof(LinearAxis)))
        //            {
        //                RemoveAxis(pos);
        //                Model.Axes.Add(new LinearAxis { Position = pos, Angle = -45 });
        //            }
        //            break;
        //    }

        //    Model.InvalidatePlot(false);

        //}

        public SciChartBase() : base()
        {

            ToolTipBackground  = new SolidColorBrush(Color.FromArgb(128, 255, 255, 255));
            ToolTipForeground  = new SolidColorBrush(Color.FromRgb(0, 0, 0));
            MajorGridLineColor = new SolidColorBrush(Color.FromRgb(0, 0, 0));
            MinorGridLineColor = new SolidColorBrush(Color.FromRgb(0, 0, 0));
            IsLegendVisible = false;

            //SetAxis(AxisPosition.Bottom, typeof(double));
            //SetAxis(AxisPosition.Left, typeof(double));
            
            //Model.InvalidatePlot(false);

            //Model.Background = OxyColor.FromArgb(0, 0, 0, 0);
            
            #region Wrapped Bindings

            SetBinding(LegendBorderThicknessProperty, new Binding("Value")
            {
                Source = new Utilities.BindingWrapper<double>(

                thickness => {

                    Model.LegendBorderThickness = thickness;
                    return thickness;
                }
            ),
                Mode = BindingMode.OneWayToSource
            });

            SetBinding(LegendOrientationProperty, new Binding("Value")
            {
                Source = new Utilities.BindingWrapper<LegendOrientation>(

                orientation => {

                    Model.LegendOrientation = orientation;
                    return orientation;
                }
            ),
                Mode = BindingMode.OneWayToSource
            });

            SetBinding(LegendPlacementProperty, new Binding("Value")
            {
                Source = new Utilities.BindingWrapper<LegendPlacement>(

                placement => {

                    Model.LegendPlacement = placement;
                    return placement;
                }
            ),
                Mode = BindingMode.OneWayToSource
            });

            SetBinding(LegendPositionProperty, new Binding("Value")
            {
                Source = new Utilities.BindingWrapper<LegendPosition>(

                position => {

                    Model.LegendPosition = position;
                    return position;
                }
            ),
                Mode = BindingMode.OneWayToSource
            });

            SetBinding(LegendBackgroundProperty, new Binding("Value")
            {
                Source = new Utilities.BindingWrapper<OxyColor>(

                color => {

                    Model.LegendBackground = color;
                    return color;
                }
            ),
                Mode = BindingMode.OneWayToSource,
                Converter = OxyColorConverter
            });

            SetBinding(PlotAreaBorderThicknessProperty, new Binding("Value")
            {
                Source = new Utilities.BindingWrapper<OxyThickness>(

                thickness => {
                    Model.PlotAreaBorderThickness = thickness;
                    return thickness;
                }

            ),
                Mode = BindingMode.OneWayToSource
            });


            SetBinding(FontSizeProperty, new Binding("Value") { Source = new Utilities.BindingWrapper<double>(
                
                size => {
                    Model.DefaultFontSize = size;
                    return size;
                }

            ), Mode = BindingMode.OneWayToSource });


            SetBinding(ForegroundProperty, new Binding("Value") { Source = new Utilities.BindingWrapper<OxyColor>(

                color => {

                    Model.TextColor = Model.PlotAreaBorderColor = color;                                        
                    return color;

                }
            ), Mode = BindingMode.OneWayToSource, Converter = OxyColorConverter });
            

            SetBinding(MajorGridlineStyleProperty, new Binding("Value")
            {
                Source = new Utilities.BindingWrapper<LineStyle>(

                    style => {
                        
                        foreach (Axis axis in Model.Axes)
                            axis.MajorGridlineStyle = style;

                        return style;
                    }

                ),
                Mode = BindingMode.OneWayToSource
            });

            SetBinding(MajorGridLineColorProperty, new Binding("Value")
            {
                Source = new Utilities.BindingWrapper<OxyColor>(

                color => {

                    foreach (Axis axis in Model.Axes)
                        axis.MajorGridlineColor = color;

                    return color;

                }
            ),
                Mode = BindingMode.OneWayToSource,
                Converter = OxyColorConverter
            });

            SetBinding(MajorGridlineThicknessProperty, new Binding("Value")
            {
                Source = new Utilities.BindingWrapper<double>(

                thickness => {

                    foreach (Axis axis in Model.Axes)
                        axis.MajorGridlineThickness = thickness;

                    return thickness;

                }
            ),
                Mode = BindingMode.OneWayToSource
            });

            SetBinding(MajorStepProperty, new Binding("Value")
            {
                Source = new Utilities.BindingWrapper<double>(

                step => {

                    foreach (Axis axis in Model.Axes)
                        axis.MajorStep = step;

                    return step;

                }
            ),
                Mode = BindingMode.OneWayToSource
            });

            SetBinding(MajorTickSizeProperty, new Binding("Value")
            {
                Source = new Utilities.BindingWrapper<double>(

                size => {

                    foreach (Axis axis in Model.Axes)
                        axis.MajorTickSize = size;

                    return size;

                }
            ),
                Mode = BindingMode.OneWayToSource
            });

            SetBinding(MinorGridlineStyleProperty, new Binding("Value")
            {
                Source = new Utilities.BindingWrapper<LineStyle>(

                        style => {

                            foreach (Axis axis in Model.Axes)
                                axis.MinorGridlineStyle = style;

                            return style;
                        }

                    ),
                Mode = BindingMode.OneWayToSource
            });

            SetBinding(MinorGridLineColorProperty, new Binding("Value")
            {
                Source = new Utilities.BindingWrapper<OxyColor>(

                        color => {

                            foreach (Axis axis in Model.Axes)
                                axis.MinorGridlineColor = color;

                            return color;
                        }

                    ),
                Mode = BindingMode.OneWayToSource,
                Converter = OxyColorConverter
            });

            SetBinding(MinorGridlineThicknessProperty, new Binding("Value")
            {
                Source = new Utilities.BindingWrapper<double>(

                thickness => {

                    foreach (Axis axis in Model.Axes)
                        axis.MinorGridlineThickness = thickness;

                    return thickness;

                }
            ),
                Mode = BindingMode.OneWayToSource
            });

            SetBinding(MinorStepProperty, new Binding("Value")
            {
                Source = new Utilities.BindingWrapper<double>(

                step => {

                    foreach (Axis axis in Model.Axes)
                        axis.MinorStep = step;

                    return step;

                }
            ),
                Mode = BindingMode.OneWayToSource
            });

            SetBinding(MinorTickSizeProperty, new Binding("Value")
            {
                Source = new Utilities.BindingWrapper<double>(

                size => {

                    foreach (Axis axis in Model.Axes)
                        axis.MinorTickSize = size;
                    
                    return size;

                }
            ),
                Mode = BindingMode.OneWayToSource
            });

            SetBinding(MaximumProperty, new Binding("Value")
            {
                Source = new Utilities.BindingWrapper<double>(

                max => {

                    foreach (Axis axis in Model.Axes)
                    {
                        if( !(axis is DateTimeAxis) )
                        {
                            axis.Maximum = max;
                        }
                    }
                        

                    return max;

                }
            ),
                Mode = BindingMode.OneWayToSource
            });

            SetBinding(MinimumProperty, new Binding("Value")
            {
                Source = new Utilities.BindingWrapper<double>(

                min => {

                    foreach (Axis axis in Model.Axes)
                    {
                        if (!(axis is DateTimeAxis))
                        {
                            axis.Minimum = min;
                        }
                    }


                    return min;

                }
            ),
                Mode = BindingMode.OneWayToSource
            });

            SetBinding(YAxisLabelProperty, new Binding("Value")
            {
                Source = new Utilities.BindingWrapper<string>(

                label => {

                    foreach (Axis axis in Model.Axes)
                    {
                        if (axis.Position == AxisPosition.Left)
                        {
                            axis.Title = label;
                        }
                    }


                    return label;

                }
            ),
                Mode = BindingMode.OneWayToSource
            });


            SetBinding(XAxisLabelProperty, new Binding("Value")
            {
                Source = new Utilities.BindingWrapper<string>(

                label => {

                    foreach (Axis axis in Model.Axes)
                    {
                        if (axis.Position == AxisPosition.Bottom)
                        {
                            axis.Title = label;
                        }
                    }


                    return label;

                }
            ),
                Mode = BindingMode.OneWayToSource
            });



            #endregion

            Properties.Add(new PropertyDefinition()
            {
               
                Category = "Plot",
                TargetProperties = new List<object>(new string[] {
                    "Colors", "ToolTipBackground", "ToolTipForeground",
                    "MinorGridlineStyle", "MinorGridLineColor", "MinorGridlineThickness", "MinorStep", "MinorTickSize",
                    "MajorGridlineStyle", "MajorGridLineColor", "MajorGridlineThickness", "MajorStep", "MajorTickSize",
                    "IsLegendVisible", "LegendBackground", "LegendBorderThickness", "LegendOrientation", "LegendPlacement", "LegendPosition"
                })
            });

            Properties.Add(new PropertyDefinition()
            {
                IsExpandable = true,
                Category = "Plot",
                TargetProperties = new List<object>(new string[] {
                    "PlotAreaBorderThickness"
                })
            });

            Properties.Add(new PropertyDefinition()
            {
                IsExpandable = false,
                Category = "Plot.Axis",
                TargetProperties = new List<object>(new string[] {
                    "Maximum", "Minimum", "XAxisLabel", "YAxisLabel"
                })
            });

            PropertyChanged += delegate
            {
                Dispatcher.InvokeAsync(Refresh);
            };

            Controls.CollectionChanged += Controls_CollectionChanged;

            RegisterUserCommand();
            RegisterUserCommand("Reset View", new RelayCommand(ExecuteCommand, canExecute, CommandType.RESET));

        }
                
        private new enum CommandType
        {
            RESET = 100
        }

        protected override void ExecuteCommand(object sender, object parameter)
        {

            RelayCommand cmd = (RelayCommand)sender;

            switch ((CommandType)cmd.Arguments[0].Value)
            {
                case CommandType.RESET:
                    Surface.ZoomExtents();
                    break;
            }

            base.ExecuteCommand(sender, parameter);

        }
        
        public override void Dispose()
        {

            Controls.CollectionChanged -= Controls_CollectionChanged;

            foreach (MultiControl m in Controls)
            {

                m.DataChanged -= DataChanged;
                m.Dispose();
            }

            base.Dispose();
        }

        protected List<KeyValuePair<KeyItem, T>> Series = new List<KeyValuePair<KeyItem, T>>();        

        protected virtual void Controls_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {

            IList items = e.OldItems;

            if (items != null)
            {
                foreach (MultiControl m in items)
                {
                    Series.RemoveAll(s => {

                        if (s.Key.Path == m.Key.Path && Surface.RenderableSeries.Remove((BaseRenderableSeries)(object)s.Value))
                        {

                            m.DataChanged  -= DataChanged;
                            OnSeriesRemoved(m.Key, m, s.Value);

                            return true;
                        }

                        return false;

                    });
                }
            }

            items = e.NewItems;

            if (items != null)
            {

                foreach (MultiControl m in items)
                {

                    if (this.Series.Count(s => { return s.Key.Path == m.Key.Path; }) == 0)
                    {
                        IRenderableSeries series = CreateInstance(m);

                        if (series == null) return;
                        
                        this.Series.Add(new KeyValuePair<KeyItem, T>(m.Key, (T)(object)series));

                        Surface.RenderableSeries.Add(series);

                        OnSeriesAdded(m.Key, m, (T)(object)series);

                        m.DataChanged += DataChanged;
                    }
                }
            }
        }

        public override void Reload()
        {
            base.Reload();
        }

        protected virtual IRenderableSeries CreateInstance(MultiControl control)
        {

            IRenderableSeries s = (IRenderableSeries)Activator.CreateInstance(typeof(T));
            
            s.SeriesName = control.Key.Title.ToString();
            //s.ItemsSource = control.Entries;
            //s.Background = OxyColor.FromArgb(0, 0, 0, 0);

            return s;
        }       

        
    }

}


