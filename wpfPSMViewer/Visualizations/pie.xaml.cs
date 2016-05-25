using System;
using System.Collections.Generic;
using System.Linq;
using OxyPlot.Series;
using OxyPlot.Axes;
using PSMViewer.Models;
using PSMViewer.ViewModels;
using OxyPlot;
using System.Windows.Data;
using System.Collections.Specialized;
using PSMonitor;

namespace PSMViewer.Visualizations
{
    [Visible(true)]
    [DisplayName("Pie (OxyPlot)")]
    [Icon("../icons/chart_pie.png")]
    public sealed partial class Pie : OxyBase<PieSeries>
    {

        public static string DisplayName { get { return typeof(Pie).Name; } }
        public static string Icon { get; private set; } = "../icons/chart_pie.png";

        public class PieSliceItem : EntryItem
        {
            private Pie _pie;
            private KeyItem _key;

            public KeyItem Key
            {
                get
                {
                    return _key;
                }
            }

            public KeyItemTitle Title
            {
                get
                {
                    return _key.Title;
                }
            }

            public double ConvertedValue
            {
                get
                {
                    return _key.Convert<double>(entry);
                }
            }

            public PieSliceItem(KeyItem key, Entry entry, Pie pie) : base(entry) {
                this._pie = pie;
                this._key = key;
            }

            public PieSliceItem(KeyItem key, EntryItem item, Pie pie) : base(item) {
                this._pie = pie;
                this._key = key;
            }

        }

        public Pie()
        {           

            Model.Axes.Clear();            
            InitializeComponent();

            PieSeries s = _series;

            s.InsideLabelPosition = .8;
            s.Background = OxyColor.FromArgb(0, 0, 0, 0);

            s.ItemsSource = Enumerable.SelectMany<MultiControl, PieSliceItem>(Controls, m => {
                return m.Entries.Select(entryitem => {
                    return new PieSliceItem(m.Key, entryitem, this);
                });
            });

            s.LabelField = "Title";
            s.ValueField = "ConvertedValue";
        }

        private PieSeries _series = new PieSeries();

        public override void Remove(KeyItem key)
        {
            base.Remove(key);

            if(!Model.Series.Contains(_series))
                Model.Series.Add(_series);

            Model.InvalidatePlot(true);
        }

        protected override Series CreateInstance(MultiControl control)
        {
            
            Model.InvalidatePlot(true);
            Model.Series.Remove(_series);

            return _series;
        }

        public sealed override void Dispose()
        {
            base.Dispose();
        }

        protected override void SetAxis(AxisPosition pos, Type type = null)
        {
         
        }


    }
}
