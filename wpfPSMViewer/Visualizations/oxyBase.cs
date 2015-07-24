using System;
using System.Collections.Generic;
using System.Linq;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Axes;
using PSMViewer.Models;
using PSMViewer.ViewModels;
using System.Collections;

namespace PSMViewer.Visualizations
{
    
    public class OxyBase<T> : VisualizationControl, IDisposable
    {

        public static bool isVisible { get { return false; } }

        public PlotModel Model { get; private set; } = new PlotModel() { Title = "" };
        public PlotController Controller { get; private set; } = new PlotController();

        public override string Title
        {
            get
            {
                return Model.Title;
            }

            set
            {
                Model.Title = value;
            }
        }

        public override void Refresh()
        {
            Model.InvalidatePlot(true);
            base.Refresh();
        }

        protected void DataChanged(object sender)
        {
            Refresh();
        }

        public OxyBase()
        {

            Controls.CollectionChanged += Controls_CollectionChanged;

            Model.Axes.Add(new DateTimeAxis { Position = AxisPosition.Bottom, Angle = 45, Title = "Time" });
            Model.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "Value" });

            foreach(Axis axis in Model.Axes)
            {
                axis.MinorGridlineStyle = LineStyle.Dot;
                axis.MajorGridlineStyle = LineStyle.Solid;
            }

            Model.InvalidatePlot(false);


            
        }        

        public override void Dispose()
        {

            Controls.CollectionChanged -= Controls_CollectionChanged;

            foreach (KeyValuePair<KeyItem, Controls> item in Controls)
            {
                item.Value.DataChanged -= DataChanged;
                item.Value.Dispose();
            }

            base.Dispose();
        }

        protected List<KeyValuePair<KeyItem, T>> Series = new List<KeyValuePair<KeyItem, T>>();        

        protected virtual void Controls_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            IList items = e.OldItems;

            if (items != null)
            {
                foreach (KeyValuePair<KeyItem, Controls> item in items)
                {
                    Series.RemoveAll(s => {

                        if (s.Key.Path == item.Key.Path && Model.Series.Remove((Series)Convert.ChangeType(s.Value, typeof(Series))))
                        {
                            item.Value.DataChanged -= DataChanged;
                        }

                        return false;

                    });
                }
            }

            items = e.NewItems;

            if (items != null)
            {

                foreach (KeyValuePair<KeyItem, Controls> item in items)
                {

                    if (this.Series.Count(s => { return s.Key.Path == item.Key.Path; }) == 0)
                    {

                        var series = (XYAxisSeries)Activator.CreateInstance(typeof(T));

                        series.ItemsSource = item.Value.Entries;

                        if(typeof(T).IsSubclassOf(typeof(DataPointSeries)))
                            ((DataPointSeries)series).Mapping = (obj) => {

                                EntryItem entry = (EntryItem)obj;

                                double value = 0D;

                                switch (entry.Value.GetType().Name.ToLower())
                                {
                                    case "string":
                                        value = Convert.ToDouble(((string)entry.Value).Length);
                                        break;
                                    default:
                                        value = Convert.ToDouble(entry.Value);
                                        break;
                                }

                                return new DataPoint(DateTimeAxis.ToDouble(entry.Timestamp), value);
                            };

                        this.Series.Add(new KeyValuePair<KeyItem, T>(item.Key, (T)(object)series));

                        Model.Series.Add(series);

                        item.Value.DataChanged += DataChanged;
                    }
                }
            }
        }

        
    }

}


