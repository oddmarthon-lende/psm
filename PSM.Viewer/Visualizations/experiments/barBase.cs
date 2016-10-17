/// <copyright file="barBase.cs" company="Baker Hughes Incorporated">
/// Copyright (c) 2015 All Rights Reserved
/// </copyright>
/// <author>Odd Marthon Lende</author>
/// <summary>A base for widgets using OxyPlot (Column, Bar)</summary>
/// 

using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using PSMonitor;
using PSMViewer.Dialogs;
using PSMViewer.Models;
using PSMViewer.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace PSMViewer.Visualizations
{
    public class BarBase<T, TItem> : OxyBase<T>
    {

        public KeyItemPathList Groups { get; set; } = new KeyItemPathList();

        protected CategoryAxis CategoryAxis = new CategoryAxis() { Position = AxisPosition.Bottom, Angle = 45 };

        public BarBase()
        {
            
            CategoryAxis.LabelField = "Value";
            CategoryAxis.ItemsSource = Controls.Select(p =>
            {
                return p.Key.Title;
            });

            RegisterUserCommand("Modify Group Title", new RelayCommand(delegate
            {

                List<KeyEditor.Item> items = new List<KeyEditor.Item>();
                List<KeyItem> parents = new List<KeyItem>();

                foreach (dynamic s in _series)
                {

                    KeyItem parent = KeyItem.CreateFromPath(s.Key);
                    parents.Add(parent);

                    if (!Groups.Contains(s.Key))
                        Groups.Add(new KeyItemPath(s.Key));
                    else
                        parent.Title.Position = Groups.Get(s.Key).Position ?? parent.Title.Position;

                    parent.Title.PropertyChanged += (a, b) => Refresh();
                }
            

                foreach (KeyItem k in parents)
                {
                    items.Add(new KeyEditor.Item(k));
                }

                KeyEditor window = new KeyEditor(items.ToArray());
                window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                window.Owner = this.Owner;

                window.ShowDialog();

            }, delegate
            {
                return true;
            }));

            #region Bindings

            SetBinding(MinorGridlineStyleProperty, new Binding("Value")
            {
                Source = new Utilities.BindingWrapper<LineStyle>(

                        style => {
                            CategoryAxis.MinorGridlineStyle = style;
                            return style;
                        }

                    ),
                Mode = BindingMode.OneWayToSource
            });

            SetBinding(MajorGridlineStyleProperty, new Binding("Value")
            {
                Source = new Utilities.BindingWrapper<LineStyle>(

                    style => {
                        CategoryAxis.MajorGridlineStyle = style;
                        return style;
                    }

                ),
                Mode = BindingMode.OneWayToSource
            });

            #endregion

        }

        protected List<string> Categories = new List<string>();

        public override void Refresh()
        {

            base.Refresh();

            foreach (KeyValuePair<KeyItem, T> s in Series)
            {
                ((Series)(object)s.Value).Title = (s.Key.Parent == null ? "*" : s.Key.Parent.Name);
            }

            foreach(dynamic s in _series)
            {

                s.Value.Title = (s.Key == "*" ? s.Key : KeyItem.CreateFromPath(s.Key).Title.Value);

                if (Groups.Contains(s.Key))
                    s.Value.Title.Position = Groups.Get(s.Key).Position ?? s.Value.Title.Position;

            }

            Categories.Clear();

            foreach (KeyItemTitle t in CategoryAxis.ItemsSource)
            {
                Categories.Add(t.Value.ToLower());
            }

            
            Model.InvalidatePlot(false);

        }

        protected override void SetAxis(AxisPosition pos, Type type = null)
        {
            if(pos == AxisPosition.Bottom)
            {
                if (GetAxis(pos) == null)
                    Model.Axes.Add(CategoryAxis);
            }
            else
                base.SetAxis(pos, type);
        }
        protected Dictionary<string, T> _series = new Dictionary<string, T>();

        public override void Remove(KeyItem key)
        {

            base.Remove(key);

            if (Controls.Count(m => { return m.Key.Parent.Path == key.Parent.Path; }) > 0)
            {
                Series s = (Series)(object)_series[key.Parent.Path];

                if(!Model.Series.Contains(s))
                    Model.Series.Add(s);
            }
            else
            {
                _series.Remove(key.Parent.Path);
            }
        }

        protected override Series CreateInstance(MultiControl control)
        {
            

            string path = (control.Key.Parent == null ? "" : control.Key.Parent.StaticPath).ToLower();
            T s;

            if (!_series.TryGetValue(path, out s))
            {
                s = (T)Activator.CreateInstance(typeof(T));
                _series.Add(path, (T)(object)s);

                BarSeriesBase bar = ((BarSeriesBase)(object)s);

                bar.Title = (control.Key.Parent == null ? "*" : control.Key.Parent.Title.Value);
                bar.Background = OxyColor.FromArgb(0, 0, 0, 0);

                bar.ItemsSource = Controls.Where(c => {
                    return (c.Key.Parent == null ? "" : c.Key.Parent.StaticPath).ToLower() == path;
                }).SelectMany(m =>
                {
                    
                    return m.Entries.Select(entry =>
                    {
                        
                        dynamic item = Activator.CreateInstance(typeof(TItem));

                        item.CategoryIndex = Categories.IndexOf(m.Key.Name.ToLower());
                        item.Value = m.Key.Convert<double>((Entry)entry);

                        return (TItem)item;
                    });

                });

            }
            else
            {
                Model.Series.Remove((Series)(object)s);
            }

            return (Series)(object)s;

        }

    }
}
