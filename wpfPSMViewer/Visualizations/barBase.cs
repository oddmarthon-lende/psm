using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using PSMViewer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Data;

namespace PSMViewer.Visualizations
{
    public class BarBase<T, TItem> : OxyBase<T>
    {

        protected CategoryAxis CategoryAxis;

        public BarBase()
        {

            Model.Axes[0] = CategoryAxis = new CategoryAxis() { Position = AxisPosition.Bottom, Angle = 45 };

            CategoryAxis.LabelField = "Name";
            CategoryAxis.ItemsSource = Controls.Select(p =>
            {
                return p.Key;
            });

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

        protected List<string> Categories = null;

        public override void Refresh()
        {
            base.Refresh();
            Categories = ((IEnumerable<KeyItem>)CategoryAxis.ItemsSource).Select(k => { return k.Name.ToLower(); }).ToArray().ToList();
        }

        protected Dictionary<string, T> _series = new Dictionary<string, T>();

        public override void Remove(KeyItem key)
        {

            base.Remove(key);

            if (Controls.Count(m => { return m.Key.Parent.Path == key.Parent.Path; }) > 0)
            {
                Model.Series.Add((Series)(object)_series[key.Parent.Path]);
            }
            else
            {
                _series.Remove(key.Parent.Path);
            }
        }

        protected override Series CreateInstance(MultiControl control)
        {
            

            string path = (control.Key.Parent == null ? "" : control.Key.Parent.Path).ToLower();
            T s;

            if (!_series.TryGetValue(path, out s))
            {
                s = (T)Activator.CreateInstance(typeof(T));
                _series.Add(path, (T)(object)s);

                BarSeriesBase bar = ((BarSeriesBase)(object)s);

                bar.Title = (control.Key.Parent == null ? "*" : control.Key.Parent.Name);
                bar.Background = OxyColor.FromArgb(0, 0, 0, 0);

                bar.ItemsSource = Controls.Where(c => {
                    return (c.Key.Parent == null ? "" : c.Key.Parent.Path).ToLower() == path;
                }).SelectMany(m =>
                {
                    
                    return m.Entries.Select(entry =>
                    {
                        
                        dynamic item = Activator.CreateInstance(typeof(TItem));

                        item.CategoryIndex = Categories.IndexOf(m.Key.Name.ToLower());
                        item.Value = ConvertEntryValueToDouble(entry);

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
