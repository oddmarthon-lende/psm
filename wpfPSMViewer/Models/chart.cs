using PSMonitor;
using PSMViewer.Models;
using PSMViewer.Properties;
using PSMViewer.ViewModels;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls.DataVisualization.Charting;
using System.Windows.Data;

namespace PSMViewer
{

    public class Chart : System.Windows.Controls.DataVisualization.Charting.Chart
    {
        private static Assembly toolkit = Assembly.Load(new AssemblyName("System.Windows.Controls.DataVisualization.Toolkit"));

        public static Type SetChartType(KeyItem key, Type chartType)
        {

            Settings s = Settings.Default;

            foreach (string t in (from string t in s.chartType
                                  where t.StartsWith(key.Path)
                                  select t).ToArray())
            {
                s.chartType.Remove(t);
            }

            s.chartType.Add(String.Format("{0},{1}", key.Path, chartType.FullName));
            s.Save();

            return chartType;
        }

        public static Type GetChartType(KeyItem key)
        {

            Settings.Default.chartType = Settings.Default.chartType ?? new StringCollection();

            Type chartType = (from string t in Settings.Default.chartType
                              where (t.StartsWith(key.Path))
                              select toolkit.GetType(t.Split(',')[1])).ElementAtOrDefault(0);

            return chartType ?? SetChartType(key, typeof(AreaSeries));

        }
        
        public Chart ()
        {
            
        }

        public void Remove(KeyItem key)
        {

            ISeries c = (from s in Series where (((KeyItem)((DataPointSeries)s).GetBindingExpression(DataPointSeries.TitleProperty).DataItem).Path == key.Path) select s).ElementAtOrDefault(0);

            if(c != null)
            {
                Series.Remove(c);
                key.Children.CollectionChanged -= Children_CollectionChanged;
            }
                
        }

        private ObservableCollection<KeyValuePair<KeyItem, Controls>> controls = new ObservableCollection<KeyValuePair<KeyItem, Controls>>();
        public ObservableCollection<KeyValuePair<KeyItem, Controls>> Controls {

            get
            {
                return controls;
            }

        }

        private TimeSpan timespan = new TimeSpan(365, 0, 0, 0, 0);
        public TimeSpan Timespan
        {
            get
            {
                return timespan;
            }
            private set
            {
                timespan = value;

                foreach (Controls control in (from control in controls select control.Value))
                {
                    control.Count = timespan;
                }
            }
        }

        public void Reload()
        {
            DateTime now = DateTime.Now;

            foreach ( Controls control in (from control in controls select control.Value))
            {
                control.Start = now.Subtract(timespan);
                control.Reload();
            }

        }

        private void Children_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            
            foreach (Controls c in (from s in controls select s.Value))
            {
                if (e.OldItems != null && (from KeyItem k in e.OldItems where k.Path == c.Selected.Path select k).ElementAtOrDefault(0) == null )
                    Remove(c.Selected);
            }

            if (e.NewItems == null) return;

            foreach (KeyItem k in e.NewItems)
            {
                if (k.Type != null && (from s in controls where s.Key.Path == k.Path select s.Value).ElementAtOrDefault(0) == null)
                    Add(k);
            }
        }

        public DataPointSeries Add(KeyItem key)
        {

            if (key != null)
            {

                if(key.Type == null)
                {
                    key.Children.CollectionChanged += Children_CollectionChanged;
                    Dispatcher.InvokeAsync(key.Reload);
                    return null;
                }

                Type chartType = GetChartType(key);

                Remove(key);

                DataPointSeries ps = (DataPointSeries)Activator.CreateInstance(chartType);

                Controls c = (from s in controls where s.Key.Path == key.Path select s.Value).ElementAtOrDefault(0);

                if(c == null)
                {
                    c = new Controls<DateTime, TimeSpan>(new ObservableCollection<EntryItem>(), DateTime.Now, timespan) { Selected = key };
                    controls.Add(new KeyValuePair<KeyItem, Controls>(key, c));
                }               

                ps.SetBinding(DataPointSeries.DataContextProperty, new Binding("DataContext") { Source = this });
                ps.SetBinding(DataPointSeries.ItemsSourceProperty, new Binding("Entries") { Source = c, Mode = BindingMode.OneTime });

                switch (chartType.Name)
                {
                    case "BubbleSeries":
                        ((BubbleSeries)ps).SizeValuePath = "Age";
                        break;
                    default:
                        break;
                }

                ps.DependentValuePath = "Value";
                ps.IndependentValuePath = "Timestamp";

                Series.Add(ps);

                ps.SetBinding(DataPointSeries.TitleProperty, new Binding("Name") { Source = key });
                                
                return ps;

            }

            return null;

        }
    }

}
