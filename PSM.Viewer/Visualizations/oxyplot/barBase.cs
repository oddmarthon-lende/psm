/// <copyright file="barBase.cs" company="Baker Hughes Incorporated">
/// Copyright (c) 2016 All Rights Reserved
/// </copyright>
/// <author>Odd Marthon Lende</author>
/// <summary>A base for widgets using OxyPlot (Column, Bar)</summary>
/// 

using OxyPlot;
using OxyPlot.Axes;
using PSM.Viewer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Data;

namespace PSM.Viewer.Visualizations
{

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TItem"></typeparam>
    public class BarBase<T, TItem> : OxyBase<T>
    {
        
        /// <summary>
        /// 
        /// </summary>
        protected CategoryAxis _categoryAxis = new CategoryAxis() { Position = AxisPosition.Bottom, Angle = 45 };

        /// <summary>
        /// 
        /// </summary>
        protected Dictionary<IKeyItem, T> _series = new Dictionary<IKeyItem, T>();

        /// <summary>
        /// 
        /// </summary>
        protected List<string> _categories = new List<string>();

        /// <summary>
        /// Constructor
        /// </summary>
        public BarBase()
        {
            
                        
            #region Bindings

            SetBinding(MinorGridlineStyleProperty, new Binding("Value")
            {
                Source = new Utilities.BindingWrapper<LineStyle>(

                        style => {
                            _categoryAxis.MinorGridlineStyle = style;
                            return style;
                        }

                    ),
                Mode = BindingMode.OneWayToSource
            });

            SetBinding(MajorGridlineStyleProperty, new Binding("Value")
            {
                Source = new Utilities.BindingWrapper<LineStyle>(

                    style => {
                        _categoryAxis.MajorGridlineStyle = style;
                        return style;
                    }

                ),
                Mode = BindingMode.OneWayToSource
            });

            #endregion

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="control"></param>
        /// <returns></returns>
        protected string GetGroupIndex(IKeyItem key)
        {

            return key.W != null ? key.W.Path : key.Path;
        }        

        /// <summary>
        /// 
        /// </summary>
        public override void Refresh()
        {
            
            base.Refresh();
                        
            _categories.Clear();

            foreach(var s in _series)
            {

                OxyPlot.Series.BarSeriesBase bar = (OxyPlot.Series.BarSeriesBase)(object)s.Value;
                System.Windows.Media.Color color = (System.Windows.Media.Color)KeyColorPropertyDescriptor.GetValue(s.Key);

                bar.Title = s.Key.Title.Value;
                bar.FillColor = OxyColor.FromArgb(color.A, color.R, color.G, color.B);

            }

            foreach (MultiControl c in Controls)
            {
                if(!_categories.Contains(c.Key.Title.Value))
                    _categories.Add(c.Key.Title.Value);
            }

            _categoryAxis.ItemsSource = _categories;
            
            Model.InvalidatePlot(true);

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="title"></param>
        /// <param name="value"></param>
        /// <param name="index"></param>
        /// <param name="hit"></param>
        /// <returns></returns>
        protected override string FormatStatusBarText(string title, object value, object index, TrackerHitResult hit)
        {
            return String.Format("{0} [{1}] ({2}, {3})", value, ((IEntry)hit.Item).Index, _categories[((OxyPlot.Series.CategorizedItem)hit.Item).CategoryIndex], title);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="type"></param>
        protected override void SetAxis(AxisPosition pos, Type type = null)
        {
            if(pos == AxisPosition.Bottom)
            {
                if (GetAxis(pos) == null)
                    Model.Axes.Add(_categoryAxis);
            }
            else
                base.SetAxis(pos, type);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public override bool Remove(KeyItem key)
        {

            bool result = base.Remove(key);
            string path = GetGroupIndex(key);
            IKeyItem pk = key.W != null ? key.W : key;

            if (Controls.Count(m => GetGroupIndex(m.Key) == path) > 0)
            {
                OxyPlot.Series.Series s = (OxyPlot.Series.Series)(object)_series[pk];

                if(!Model.Series.Contains(s))
                    Model.Series.Add(s);
            }
            else
            {
                _series.Remove(pk);
            }

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="control"></param>
        /// <returns></returns>
        protected override OxyPlot.Series.Series CreateInstance(MultiControl control)
        {


            string path = GetGroupIndex(control.Key);
            IKeyItem pk = control.Key.W != null ? control.Key.W : control.Key;
            T s;
           
            if (!_series.TryGetValue(pk, out s))
            {

                s = (T)typeof(T).New();
                _series.Add(pk, s);

                OxyPlot.Series.BarSeriesBase bar = ((OxyPlot.Series.BarSeriesBase)(object)s);

                bar.Background = OxyColor.FromArgb(0, 0, 0, 0);
                bar.ItemsSource = Controls.Where(c => GetGroupIndex(c.Key) == path)
                .SelectMany(m => m.Entries.Select(entry =>
                {

                    dynamic item = typeof(TItem).New();
                    System.Windows.Media.Color color = (System.Windows.Media.Color)KeyColorPropertyDescriptor.GetValue(m.Key);

                    item.Color = OxyColor.FromArgb(color.A, color.R, color.G, color.B);

                    if (!_categories.Contains(m.Key.Title.Value))
                        _categories.Add(m.Key.Title.Value);

                    item.CategoryIndex = _categories.IndexOf(m.Key.Title.Value);
                    item.Value = m.Key.Convert<double>((Entry)entry);
                    item.Index = entry.Index;
                                        
                    return (TItem)item;

                }));

            }
            else
            {
                Model.Series.Remove((OxyPlot.Series.Series)(object)s);
            }

            return (OxyPlot.Series.Series)(object)s;

        }

    }
}
