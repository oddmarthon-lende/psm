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
using OxyPlot.Series;
using OxyPlot.Axes;
using PSMViewer.Models;
using System.Collections;
using System.Windows.Media;
using Xceed.Wpf.Toolkit.PropertyGrid;
using System.Windows.Data;
using PSMViewer.Converters;
using System.Windows;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using System.Windows.Controls;

namespace PSMViewer.Visualizations
{
    
    public class SolidColorBrushList : IList<SolidColorBrush>, ICollection<SolidColorBrush>, ICollection, IList {


        private IList<OxyColor> OxyColors;
        private IValueConverter Converter = new SolidColorBrushToOxyColorConverter();
        
        public SolidColorBrushList()
        {
            OxyColors = new List<OxyColor>();
        }               

        public SolidColorBrushList(IList<OxyColor> OxyColors) {
            this.OxyColors = OxyColors;          
        }

        public void Reset (IList<OxyColor> OxyColors)
        {

            IList<OxyColor> colors = this.OxyColors.ToList();

            this.OxyColors = OxyColors;

            OxyColors.Clear();

            foreach (OxyColor color in colors)
                OxyColors.Add(color);

        }

        public SolidColorBrush this[int index]
        {
            get
            {
                return (SolidColorBrush)Converter.Convert(OxyColors[index], typeof(SolidColorBrush), null, null);
            }

            set
            {
                OxyColors[index] = (OxyColor)Converter.ConvertBack(value, typeof(OxyColor), null, null);
            }
        }

        public int Count
        {
            get
            {
                return OxyColors.Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return OxyColors.IsReadOnly;
            }
        }

        public object SyncRoot
        {
            get
            {
                return OxyColors;
            }
        }

        public bool IsSynchronized
        {
            get
            {
                return false;
            }
        }

        public bool IsFixedSize
        {
            get
            {
                return false;
            }
        }

        object IList.this[int index]
        {
            get
            {
                return this[index];
            }

            set
            {
                this[index] = (SolidColorBrush)value;
            }
        }

        public void Add(SolidColorBrush item)
        {
            OxyColors.Add((OxyColor)Converter.ConvertBack(item, typeof(OxyColor), null, null));
        }

        public void Clear()
        {
            OxyColors.Clear();
        }

        public bool Contains(SolidColorBrush item)
        {
            return OxyColors.Contains((OxyColor)Converter.ConvertBack(item, typeof(OxyColor), null, null));
        }

        public void CopyTo(SolidColorBrush[] array, int arrayIndex)
        {
            foreach(SolidColorBrush color in this)
            {
                array[arrayIndex++] = color;
            }
        }

        public IEnumerator<SolidColorBrush> GetEnumerator()
        {
            
            return OxyColors.Select(color =>
            {
                return (SolidColorBrush)Converter.Convert(color, typeof(SolidColorBrush), null, null);
            }).GetEnumerator();
        }

        public int IndexOf(SolidColorBrush item)
        {
            return OxyColors.IndexOf((OxyColor)Converter.ConvertBack(item, typeof(OxyColor), null, null));
        }

        public void Insert(int index, SolidColorBrush item)
        {
            OxyColors.Insert(index, (OxyColor)Converter.ConvertBack(item, typeof(OxyColor), null, null));
        }

        public bool Remove(SolidColorBrush item)
        {
            return OxyColors.Remove((OxyColor)Converter.ConvertBack(item, typeof(OxyColor), null, null));
        }

        public void RemoveAt(int index)
        {
            OxyColors.RemoveAt(index);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public void CopyTo(Array array, int index)
        {
            this.CopyTo((SolidColorBrush[])array, index);
        }

        public int Add(object value)
        {
            this.Add((SolidColorBrush)value);
            return 1;
        }

        public bool Contains(object value)
        {
            return this.Contains((SolidColorBrush)value);
        }

        public int IndexOf(object value)
        {
            return this.IndexOf((SolidColorBrush)value);
        }

        public void Insert(int index, object value)
        {
            this.Insert(index, (SolidColorBrush)value);
        }

        public void Remove(object value)
        {
            this.Remove((SolidColorBrush)value);
        }
    }

    public class OxyBase<T> : VisualizationControl, IDisposable
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

        public PlotModel Model { get; private set; } = new PlotModel() { Title = "" };
        public PlotController Controller { get; private set; } = new PlotController();
        
        public SolidColorBrushList Colors
        {
            get { return (SolidColorBrushList)GetValue(ColorsProperty); }
            set { SetValue(ColorsProperty, value); }
        }
        public static readonly DependencyProperty ColorsProperty =
            DependencyProperty.Register("Colors", typeof(SolidColorBrushList), typeof(OxyBase<T>), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, (sender, e) => {
                ((SolidColorBrushList)e.NewValue).Reset(((OxyBase<T>)sender).Model.DefaultColors);
            }));

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

            Model.InvalidatePlot(true);

            base.Refresh();
        }

        protected void DataChanged(object sender)
        {
            Refresh();
        }

        #region Dependency Properties

        public LineStyle MinorGridlineStyle
        {
            get { return (LineStyle)GetValue(MinorGridlineStyleProperty); }
            set { SetValue(MinorGridlineStyleProperty, value); }
        }
        public static readonly DependencyProperty MinorGridlineStyleProperty =
            DependencyProperty.Register("MinorGridlineStyle", typeof(LineStyle), typeof(OxyBase<T>), new FrameworkPropertyMetadata(LineStyle.Dot, FrameworkPropertyMetadataOptions.AffectsRender));
        
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

        
        public LineStyle MajorGridlineStyle
        {
            get { return (LineStyle)GetValue(MajorGridlineStyleProperty); }
            set { SetValue(MajorGridlineStyleProperty, value); }
        }
        public static readonly DependencyProperty MajorGridlineStyleProperty =
            DependencyProperty.Register("MajorGridlineStyle", typeof(LineStyle), typeof(OxyBase<T>), new FrameworkPropertyMetadata(LineStyle.Solid, FrameworkPropertyMetadataOptions.AffectsRender));

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


        public OxyThickness PlotAreaBorderThickness
        {
            get { return (OxyThickness)GetValue(PlotAreaBorderThicknessProperty); }
            set { SetValue(PlotAreaBorderThicknessProperty, value); }
        }
        public static readonly DependencyProperty PlotAreaBorderThicknessProperty =
            DependencyProperty.Register("PlotAreaBorderThickness", typeof(OxyThickness), typeof(OxyBase<T>), new FrameworkPropertyMetadata(new OxyThickness(0), FrameworkPropertyMetadataOptions.AffectsRender));

        [ExpandableObject]
        public SolidColorBrush LegendBackground
        {
            get { return (SolidColorBrush)GetValue(LegendBackgroundProperty); }
            set { SetValue(LegendBackgroundProperty, value); }
        }
        public static readonly DependencyProperty LegendBackgroundProperty =
            DependencyProperty.Register("LegendBackground", typeof(SolidColorBrush), typeof(OxyBase<T>), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));


        public LineStyle LineStyle
        {
            get { return (LineStyle)GetValue(LineStyleProperty); }
            set { SetValue(LineStyleProperty, value); }
        }
        public static readonly DependencyProperty LineStyleProperty =
            DependencyProperty.Register("LineStyle", typeof(LineStyle), typeof(OxyBase<T>), new FrameworkPropertyMetadata(LineStyle.Solid, FrameworkPropertyMetadataOptions.AffectsRender));



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



        public LegendOrientation LegendOrientation
        {
            get { return (LegendOrientation)GetValue(LegendOrientationProperty); }
            set { SetValue(LegendOrientationProperty, value); }
        }
        public static readonly DependencyProperty LegendOrientationProperty =
            DependencyProperty.Register("LegendOrientation", typeof(LegendOrientation), typeof(OxyBase<T>), new FrameworkPropertyMetadata(LegendOrientation.Vertical, FrameworkPropertyMetadataOptions.AffectsRender));



        public LegendPlacement LegendPlacement
        {
            get { return (LegendPlacement)GetValue(LegendPlacementProperty); }
            set { SetValue(LegendPlacementProperty, value); }
        }
        public static readonly DependencyProperty LegendPlacementProperty =
            DependencyProperty.Register("LegendPlacement", typeof(LegendPlacement), typeof(OxyBase<T>), new FrameworkPropertyMetadata(LegendPlacement.Inside, FrameworkPropertyMetadataOptions.AffectsRender));



        public LegendPosition LegendPosition
        {
            get { return (LegendPosition)GetValue(LegendPositionProperty); }
            set { SetValue(LegendPositionProperty, value); }
        }
        public static readonly DependencyProperty LegendPositionProperty =
            DependencyProperty.Register("LegendPosition", typeof(LegendPosition), typeof(OxyBase<T>), new FrameworkPropertyMetadata(LegendPosition.TopRight, FrameworkPropertyMetadataOptions.AffectsRender));


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

        public OxyBase() : base()
        {

            ToolTipBackground  = new SolidColorBrush(Color.FromArgb(128, 255, 255, 255));
            ToolTipForeground  = new SolidColorBrush(Color.FromRgb(0, 0, 0));
            MajorGridLineColor = new SolidColorBrush(Color.FromRgb(0, 0, 0));
            MinorGridLineColor = new SolidColorBrush(Color.FromRgb(0, 0, 0));

            Colors = new SolidColorBrushList(Model.DefaultColors);

            Model.Axes.Add(new DateTimeAxis { Position = AxisPosition.Bottom, Angle = 45 });
            Model.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Angle = -45 });

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
                    "Colors", "ToolTipBackground", "ToolTipForeground",
                    "MinorGridlineStyle", "MinorGridLineColor", "MinorGridlineThickness", "MinorStep", "MinorTickSize",
                    "MajorGridlineStyle", "MajorGridLineColor", "MajorGridlineThickness", "MajorStep", "MajorTickSize",
                    "LegendBackground", "LegendBorderThickness", "LegendOrientation", "LegendPlacement", "LegendPosition"
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
                    Model.ResetAllAxes();
                    break;
            }

            base.ExecuteCommand(sender, parameter);

        }
                 
        protected static SolidColorBrushToOxyColorConverter OxyColorConverter = new SolidColorBrushToOxyColorConverter();
        protected static DoubleToOxyThicknessConverter OxyThicknessConverter = new DoubleToOxyThicknessConverter();

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

                        if (s.Key.Path == m.Key.Path && Model.Series.Remove((Series)(object)s.Value))
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
                        Series series = CreateInstance(m);

                        if (series == null) return;
                                      
                        if (typeof(T).IsSubclassOf(typeof(DataPointSeries)))
                            ((DataPointSeries)series).Mapping = (obj) => {

                                EntryItem entry = (EntryItem)obj;

                                return new DataPoint(DateTimeAxis.ToDouble(entry.Timestamp), ConvertEntryValueToDouble(entry, ConversionFactor));
                            };

                        this.Series.Add(new KeyValuePair<KeyItem, T>(m.Key, (T)(object)series));

                        Model.Series.Add(series);

                        OnSeriesAdded(m.Key, m, (T)(object)series);

                        m.DataChanged += DataChanged;
                    }
                }
            }
        }

        protected virtual Series CreateInstance(MultiControl control)
        {

            XYAxisSeries s = (XYAxisSeries)Activator.CreateInstance(typeof(T));

            s.Title = control.Key.Name;
            s.ItemsSource = control.Entries;
            s.Background = OxyColor.FromArgb(0, 0, 0, 0);

            return s;
        }       

        
    }

}


