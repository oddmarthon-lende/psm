using System;
using System.Linq;
using OxyPlot.Series;
using OxyPlot.Axes;
using OxyPlot;
using System.Windows.Media;
using PSM.Viewer.Models;

namespace PSM.Viewer.Visualizations
{
    /// <summary>
    /// 
    /// </summary>
    [Visible(true)]
    [DisplayName("Pie (OxyPlot)")]
    [Icon("../icons/chart_pie.png")]
    public sealed partial class Pie : OxyBase<PieSeries>
    {

        /// <summary>
        /// 
        /// </summary>
        public class PieSliceItem : Entry
        {
            /// <summary>
            /// 
            /// </summary>
            private Pie _pie;

            /// <summary>
            /// 
            /// </summary>
            private KeyItem _key;

            /// <summary>
            /// 
            /// </summary>
            public new KeyItem Key
            {
                get
                {
                    return _key;
                }
            }

            /// <summary>
            /// 
            /// </summary>
            public string Title
            {
                get
                {
                    return _key.Title.Value;
                }
            }

            /// <summary>
            /// 
            /// </summary>
            public double ConvertedValue
            {
                get
                {
                    return _key.Convert<double>(this);
                }
            }

            /// <summary>
            /// 
            /// </summary>
            public OxyColor Color { get; set; }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="key"></param>
            /// <param name="entry"></param>
            /// <param name="pie"></param>
            public PieSliceItem(KeyItem key, Entry entry, Pie pie) : base(entry)
            {

                this._pie = pie;
                this._key = key;
                
            }
            
        }

        /// <summary>
        /// 
        /// </summary>
        private PieSeries _series = new PieSeries();

        /// <summary>
        /// 
        /// </summary>
        public Pie()
        {           

            Model.Axes.Clear();            
            InitializeComponent();

            PieSeries s = _series;

            s.InsideLabelPosition = .8;
            s.Background = OxyColor.FromArgb(0, 0, 0, 0);

            s.ItemsSource = Enumerable.SelectMany<MultiControl, PieSliceItem>(Controls, m => {
                return m.Entries.Select(entryitem => {
                    Color c = m.Key.Color;
                    return new PieSliceItem(m.Key, entryitem, this) { Color = OxyColor.FromArgb(c.A, c.R, c.G, c.B) };
                });
            });

            s.LabelField = "Title";
            s.ValueField = "ConvertedValue";
            s.ColorField = "Color";

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public override bool Remove(KeyItem key)
        {
            if(!Model.Series.Contains(_series))
                Model.Series.Add(_series);

            Model.InvalidatePlot(true);

            return base.Remove(key);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="control"></param>
        /// <returns></returns>
        protected override Series CreateInstance(MultiControl control)
        {
            
            Model.InvalidatePlot(true);
            Model.Series.Remove(_series);

            return _series;
        }

        /// <summary>
        /// 
        /// </summary>
        public sealed override void Dispose()
        {
            base.Dispose();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="type"></param>
        protected override void SetAxis(AxisPosition pos, Type type = null) { }

        protected override string FormatStatusBarText(string title, object value, object index, TrackerHitResult hit)
        {
            return base.FormatStatusBarText(((PieSliceItem)hit.Item).Title, value, index, hit);
        }

    }
}
