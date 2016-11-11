/// <copyright file="oxyBase.cs" company="Baker Hughes Incorporated">
/// Copyright (c) 2015 All Rights Reserved
/// </copyright>
/// <author>Odd Marthon Lende</author>
/// <summary>A base for all widgets using <see cref="OxyPlot"/></summary>
/// 

using System;
using System.Collections.Generic;
using System.Linq;
using OxyPlot;
using OxyPlot.Axes;
using PSM.Viewer.Models;
using System.Collections;
using System.Windows.Media;
using Xceed.Wpf.Toolkit.PropertyGrid;
using System.Windows.Data;
using PSM.Viewer.Converters;
using System.Windows;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using PSM.Viewer.Commands;
using System.Windows.Controls;

namespace PSM.Viewer.Visualizations
{

    /// <summary>
    /// PlotView with a new default tracker that only shows lines
    /// </summary>
    public class OxyPlotView : OxyPlot.Wpf.PlotView
    {

        /// <summary>
        /// Constructor
        /// </summary>
        public OxyPlotView()
        {

            FrameworkElementFactory factory = new FrameworkElementFactory(typeof(OxyPlot.Wpf.TrackerControl));

            factory.SetBinding(OxyPlot.Wpf.TrackerControl.PositionProperty, new Binding("Position"));
            factory.SetBinding(OxyPlot.Wpf.TrackerControl.LineExtentsProperty, new Binding("PlotModel.PlotArea"));
            factory.SetBinding(OxyPlot.Wpf.TrackerControl.LineStrokeProperty, new Binding("Foreground")
            {
                RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(VisualizationControl), 1)
            });
            factory.SetValue(OxyPlot.Wpf.TrackerControl.ShowPointerProperty, false);

            DefaultTrackerTemplate = new ControlTemplate()
            {
                VisualTree = factory
            };
                                    
        }
    }

    /// <summary>
    /// Base class used when creating widgets using <see cref="OxyPlot"/>
    /// </summary>
    /// <typeparam name="T">The oxyplot chart type</typeparam>
    public class OxyBase<T> : VisualizationControl, IDisposable
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="control"></param>
        /// <param name="series"></param>
        public delegate void SeriesAddedEventHandler(MultiControl control, T series);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="control"></param>
        /// <param name="series"></param>
        public delegate void SeriesRemovedEventHandler(MultiControl control, T series);

        /// <summary>
        /// 
        /// </summary>
        public event SeriesAddedEventHandler SeriesAdded;

        /// <summary>
        /// 
        /// </summary>
        public event SeriesRemovedEventHandler SeriesRemoved;
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="control"></param>
        /// <param name="series"></param>
        protected void OnSeriesRemoved(KeyItem key, MultiControl control, T series)
        {
            if (SeriesRemoved != null)
                SeriesRemoved(control, series);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="control"></param>
        /// <param name="series"></param>
        protected void OnSeriesAdded(KeyItem key, MultiControl control, T series)
        {
            if (SeriesAdded != null)
                SeriesAdded(control, series);
        }

        /// <summary>
        /// 
        /// </summary>
        public PlotModel Model { get; protected set; } = new PlotModel() { Title = "" };

        /// <summary>
        /// 
        /// </summary>
        public PlotController Controller { get; protected set; } = new PlotController();
        
        /// <summary>
        /// 
        /// </summary>
        public override void Refresh()
        {
            
            FrameworkElement Parent = (FrameworkElement)this.Parent;      

            Model.InvalidatePlot(true);

            foreach (KeyValuePair<KeyItem, T> s in Series)
            {
                ((OxyPlot.Series.Series)(object)s.Value).Title = s.Key.Title.ToString();
            }

            base.Refresh();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        protected void DataChanged(object sender)
        {
            //Refresh();
            
            foreach(MultiControl c in Controls)
            {

                if (c.Entries.Count > 0)
                {
                    SetAxis(AxisPosition.Bottom, c.Entries[0].Index[c.Get().Selected.IndexIdentifier].GetType());
                    break;
                }

                
            }

            Model.InvalidatePlot(true);
        }

        #region Dependency Properties

        /// <summary>
        /// 
        /// </summary>
        public LineStyle MinorGridlineStyle
        {
            get { return (LineStyle)GetValue(MinorGridlineStyleProperty); }
            set { SetValue(MinorGridlineStyleProperty, value); }
        }
        public static readonly DependencyProperty MinorGridlineStyleProperty =
            DependencyProperty.Register("MinorGridlineStyle", typeof(LineStyle), typeof(OxyBase<T>), new FrameworkPropertyMetadata(LineStyle.Dot, FrameworkPropertyMetadataOptions.AffectsRender));
        
        /// <summary>
        /// 
        /// </summary>
        [ExpandableObject]
        public SolidColorBrush MinorGridLineColor
        {
            get { return (SolidColorBrush)GetValue(MinorGridLineColorProperty); }
            set { SetValue(MinorGridLineColorProperty, value); }
        }
        public static readonly DependencyProperty MinorGridLineColorProperty =
            DependencyProperty.Register("MinorGridLineColor", typeof(SolidColorBrush), typeof(OxyBase<T>), new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromRgb(0, 0, 0)), FrameworkPropertyMetadataOptions.AffectsRender));
        
        /// <summary>
        /// 
        /// </summary>
        public double MinorGridlineThickness
        {
            get { return (double)GetValue(MinorGridlineThicknessProperty); }
            set { SetValue(MinorGridlineThicknessProperty, value); }
        }
        public static readonly DependencyProperty MinorGridlineThicknessProperty =
            DependencyProperty.Register("MinorGridlineThickness", typeof(double), typeof(OxyBase<T>), new FrameworkPropertyMetadata(1D, FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// 
        /// </summary>
        public double MinorStep
        {
            get { return (double)GetValue(MinorStepProperty); }
            set { SetValue(MinorStepProperty, value); }
        }
        public static readonly DependencyProperty MinorStepProperty =
            DependencyProperty.Register("MinorStep", typeof(double), typeof(OxyBase<T>), new FrameworkPropertyMetadata(double.NaN, FrameworkPropertyMetadataOptions.AffectsRender));
        
        /// <summary>
        /// 
        /// </summary>
        public double MinorTickSize
        {
            get { return (double)GetValue(MinorTickSizeProperty); }
            set { SetValue(MinorTickSizeProperty, value); }
        }
        public static readonly DependencyProperty MinorTickSizeProperty =
            DependencyProperty.Register("MinorTickSize", typeof(double), typeof(OxyBase<T>), new FrameworkPropertyMetadata(4D, FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// 
        /// </summary>
        public LineStyle MajorGridlineStyle
        {
            get { return (LineStyle)GetValue(MajorGridlineStyleProperty); }
            set { SetValue(MajorGridlineStyleProperty, value); }
        }
        public static readonly DependencyProperty MajorGridlineStyleProperty =
            DependencyProperty.Register("MajorGridlineStyle", typeof(LineStyle), typeof(OxyBase<T>), new FrameworkPropertyMetadata(LineStyle.Solid, FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// 
        /// </summary>
        [ExpandableObject]
        public SolidColorBrush MajorGridLineColor
        {
            get { return (SolidColorBrush)GetValue(MajorGridLineColorProperty); }
            set { SetValue(MajorGridLineColorProperty, value); }
        }
        public static readonly DependencyProperty MajorGridLineColorProperty =
            DependencyProperty.Register("MajorGridLineColor", typeof(SolidColorBrush), typeof(OxyBase<T>), new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromRgb(0, 0, 0)), FrameworkPropertyMetadataOptions.AffectsRender));
        
        /// <summary>
        /// 
        /// </summary>
        public double MajorGridlineThickness
        {
            get { return (double)GetValue(MajorGridlineThicknessProperty); }
            set { SetValue(MajorGridlineThicknessProperty, value); }
        }
        public static readonly DependencyProperty MajorGridlineThicknessProperty =
            DependencyProperty.Register("MajorGridlineThickness", typeof(double), typeof(OxyBase<T>), new FrameworkPropertyMetadata(1D, FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// 
        /// </summary>
        public double MajorStep
        {
            get { return (double)GetValue(MajorStepProperty); }
            set { SetValue(MajorStepProperty, value); }
        }
        public static readonly DependencyProperty MajorStepProperty =
            DependencyProperty.Register("MajorStep", typeof(double), typeof(OxyBase<T>), new FrameworkPropertyMetadata(double.NaN, FrameworkPropertyMetadataOptions.AffectsRender));
        
        /// <summary>
        /// 
        /// </summary>
        public double MajorTickSize
        {
            get { return (double)GetValue(MajorTickSizeProperty); }
            set { SetValue(MajorTickSizeProperty, value); }
        }
        public static readonly DependencyProperty MajorTickSizeProperty =
            DependencyProperty.Register("MajorTickSize", typeof(double), typeof(OxyBase<T>), new FrameworkPropertyMetadata(7D, FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// 
        /// </summary>
        public OxyThickness PlotAreaBorderThickness
        {
            get { return (OxyThickness)GetValue(PlotAreaBorderThicknessProperty); }
            set { SetValue(PlotAreaBorderThicknessProperty, value); }
        }
        public static readonly DependencyProperty PlotAreaBorderThicknessProperty =
            DependencyProperty.Register("PlotAreaBorderThickness", typeof(OxyThickness), typeof(OxyBase<T>), new FrameworkPropertyMetadata(new OxyThickness(0), FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// 
        /// </summary>
        [ExpandableObject]
        public SolidColorBrush LegendBackground
        {
            get { return (SolidColorBrush)GetValue(LegendBackgroundProperty); }
            set { SetValue(LegendBackgroundProperty, value); }
        }
        public static readonly DependencyProperty LegendBackgroundProperty =
            DependencyProperty.Register("LegendBackground", typeof(SolidColorBrush), typeof(OxyBase<T>), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// 
        /// </summary>
        public LineStyle LineStyle
        {
            get { return (LineStyle)GetValue(LineStyleProperty); }
            set { SetValue(LineStyleProperty, value); }
        }
        public static readonly DependencyProperty LineStyleProperty =
            DependencyProperty.Register("LineStyle", typeof(LineStyle), typeof(OxyBase<T>), new FrameworkPropertyMetadata(LineStyle.Solid, FrameworkPropertyMetadataOptions.AffectsRender));


        /// <summary>
        /// 
        /// </summary>
        public double StrokeThickness
        {
            get { return (double)GetValue(StrokeThicknessProperty); }
            set { SetValue(StrokeThicknessProperty, value); }
        }
        public static readonly DependencyProperty StrokeThicknessProperty =
            DependencyProperty.Register("StrokeThickness", typeof(double), typeof(OxyBase<T>), new FrameworkPropertyMetadata(1D, FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// 
        /// </summary>
        public double LegendBorderThickness
        {
            get { return (double)GetValue(LegendBorderThicknessProperty); }
            set { SetValue(LegendBorderThicknessProperty, value); }
        }
        public static readonly DependencyProperty LegendBorderThicknessProperty =
            DependencyProperty.Register("LegendBorderThickness", typeof(double), typeof(OxyBase<T>), new FrameworkPropertyMetadata(0D, FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// 
        /// </summary>
        public bool IsLegendVisible
        {
            get { return Model.IsLegendVisible; }
            set { Model.IsLegendVisible = value; Model.InvalidatePlot(true); }

        }
        
        /// <summary>
        /// 
        /// </summary>
        public LegendOrientation LegendOrientation
        {
            get { return (LegendOrientation)GetValue(LegendOrientationProperty); }
            set { SetValue(LegendOrientationProperty, value); }
        }
        public static readonly DependencyProperty LegendOrientationProperty =
            DependencyProperty.Register("LegendOrientation", typeof(LegendOrientation), typeof(OxyBase<T>), new FrameworkPropertyMetadata(LegendOrientation.Vertical, FrameworkPropertyMetadataOptions.AffectsRender));


        /// <summary>
        /// 
        /// </summary>
        public LegendPlacement LegendPlacement
        {
            get { return (LegendPlacement)GetValue(LegendPlacementProperty); }
            set { SetValue(LegendPlacementProperty, value); }
        }
        public static readonly DependencyProperty LegendPlacementProperty =
            DependencyProperty.Register("LegendPlacement", typeof(LegendPlacement), typeof(OxyBase<T>), new FrameworkPropertyMetadata(LegendPlacement.Inside, FrameworkPropertyMetadataOptions.AffectsRender));


        /// <summary>
        /// 
        /// </summary>
        public LegendPosition LegendPosition
        {
            get { return (LegendPosition)GetValue(LegendPositionProperty); }
            set { SetValue(LegendPositionProperty, value); }
        }
        public static readonly DependencyProperty LegendPositionProperty =
            DependencyProperty.Register("LegendPosition", typeof(LegendPosition), typeof(OxyBase<T>), new FrameworkPropertyMetadata(LegendPosition.TopRight, FrameworkPropertyMetadataOptions.AffectsRender));


        /// <summary>
        /// 
        /// </summary>
        public string YAxisLabel
        {
            get { return (string)GetValue(YAxisLabelProperty); }
            set { SetValue(YAxisLabelProperty, value); }
        }
        public static readonly DependencyProperty YAxisLabelProperty =
            DependencyProperty.Register("YAxisLabel", typeof(string), typeof(OxyBase<T>), new PropertyMetadata(null));


        /// <summary>
        /// 
        /// </summary>
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

        /// <summary>
        /// 
        /// </summary>
        protected static SolidColorBrushToOxyColorConverter OxyColorConverter = new SolidColorBrushToOxyColorConverter();

        /// <summary>
        /// 
        /// </summary>
        protected static DoubleToOxyThicknessConverter OxyThicknessConverter = new DoubleToOxyThicknessConverter();

        /// <summary>
        /// 
        /// </summary>
        protected List<KeyValuePair<KeyItem, T>> Series = new List<KeyValuePair<KeyItem, T>>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pos"></param>
        protected void RemoveAxis(AxisPosition pos)
        {
            foreach(Axis axis in Model.Axes.ToArray())
            {
                if (axis.Position == pos)
                {
                    Model.Axes.Remove(axis);
                }                    
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        protected Axis GetAxis(AxisPosition pos)
        {
            foreach (Axis axis in Model.Axes)
            {
                if (axis.Position == pos)
                    return axis;
            }

            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="type"></param>
        protected virtual void SetAxis(AxisPosition pos, Type type = null)
        {

            Axis axis = GetAxis(pos);

            switch(type == null ? "" : type.Name)
            {

                case "DateTime":

                    if(axis == null || !axis.GetType().Equals(typeof(DateTimeAxis)))
                    {
                        RemoveAxis(pos);
                        Model.Axes.Add(new DateTimeAxis { Position = pos, Angle = 45 });
                    }
                    break;

                default:

                    if(axis == null || !axis.GetType().Equals(typeof(LinearAxis)))
                    {
                        RemoveAxis(pos);
                        Model.Axes.Add(new LinearAxis { Position = pos, Angle = -45 });
                    }
                    break;
            }

            Model.InvalidatePlot(false);

        }

        /// <summary>
        /// Constructor
        /// </summary>
        public OxyBase() : base()
        {

            MajorGridLineColor = new SolidColorBrush(Color.FromRgb(0, 0, 0));
            MinorGridLineColor = new SolidColorBrush(Color.FromRgb(0, 0, 0));
            IsLegendVisible = false;

            SetAxis(AxisPosition.Bottom, typeof(double));
            SetAxis(AxisPosition.Left, typeof(double));

            Model.TrackerChanged += Model_TrackerChanged;

            Model.InvalidatePlot(false);

            Model.Background = OxyColor.FromArgb(0, 0, 0, 0);
            
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
                    "Colors",
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

        /// <summary>
        /// Updates the statusbar text when the tracker changes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void Model_TrackerChanged(object sender, TrackerEventArgs e)
        {

            TrackerHitResult hit = e.HitResult;

            if(hit != null && hit.Item != null)
            {
                Index index = ((IEntry)hit.Item).Index;
                
                StatusBarText = FormatStatusBarText(hit.Series.Title, ((IEntry)hit.Item).Value, index, hit);
            }
            else
                StatusBarText = "";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="title"></param>
        /// <param name="value"></param>
        /// <param name="index"></param>
        /// <param name="hit"></param>
        /// <returns></returns>
        protected virtual string FormatStatusBarText(string title, object value, object index, TrackerHitResult hit)
        {
            return String.Format("{0} ( X = {2}, Y = {1} )", title, value, index);
        }

        /// <summary>
        /// 
        /// </summary>
        private new enum CommandType
        {
            RESET = 100
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="parameter"></param>
        protected override void ExecuteCommand(object sender, object parameter)
        {

            RelayCommand cmd = (RelayCommand)sender;

            switch ((CommandType)cmd.Arguments[0].Value)
            {
                case CommandType.RESET:
                    Model.ResetAllAxes();
                    break;
            }

            base.ExecuteCommand(sender, parameter);

        }
                 
        

        /// <summary>
        /// 
        /// </summary>
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

        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void Controls_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {

            IList items = e.OldItems;

            if (items != null)
            {
                foreach (MultiControl m in items)
                {
                    Series.RemoveAll(s => {

                        if (s.Key.Path == m.Key.Path && Model.Series.Remove((OxyPlot.Series.Series)(object)s.Value))
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
                        OxyPlot.Series.Series series = CreateInstance(m);

                        if (series == null) return;
                                      
                        if (typeof(T).IsSubclassOf(typeof(OxyPlot.Series.DataPointSeries)))
                            ((OxyPlot.Series.DataPointSeries)series).Mapping = (obj) => {
                                
                                Entry entry = (Entry)obj;
                                return new DataPoint(DateTimeAxis.ToDouble(entry.Index[m.Get().Selected.IndexIdentifier]), m.Key.Convert<double>(entry));

                            };

                        this.Series.Add(new KeyValuePair<KeyItem, T>(m.Key, (T)(object)series));

                        Model.Series.Add(series);

                        OnSeriesAdded(m.Key, m, (T)(object)series);

                        m.DataChanged += DataChanged;
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public override void Reload()
        {
            base.Reload();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="control"></param>
        /// <returns></returns>
        protected virtual OxyPlot.Series.Series CreateInstance(MultiControl control)
        {

            OxyPlot.Series.XYAxisSeries s = (OxyPlot.Series.XYAxisSeries)Activator.CreateInstance(typeof(T));
            
            s.Title = control.Key.Title.ToString();
            s.ItemsSource = control.Entries;
            s.Background = OxyColor.FromArgb(0, 0, 0, 0);

            return s;
        }       

        
    }

}


