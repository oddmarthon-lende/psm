using PSMViewer.Models;
using PSMViewer.Properties;
using PSMViewer.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Windows.Controls;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PSMViewer.Visualizations
{

    public class VisualizationControl : UserControl, IDisposable
    {
        #region Static Properties and Methods
        
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
                              select Type.GetType(t.Split(',')[1])).ElementAtOrDefault(0);

            return chartType;

        }
        
        public static VisualizationControl Restore(KeyItem key)
        {
            Type t = VisualizationControl.GetChartType(key);
            return t == null ? null : (VisualizationControl)Activator.CreateInstance(t);
        }

        public class InheritorInfo : INotifyPropertyChanged
        {

            public event PropertyChangedEventHandler PropertyChanged;

            protected virtual void OnPropertyChanged(string propertyName)
            {
                PropertyChangedEventHandler handler = PropertyChanged;
                if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
            }
            protected bool SetField<TField>(ref TField field, TField value, [CallerMemberName] string propertyName = "")
            {
                if (EqualityComparer<TField>.Default.Equals(field, value)) return false;
                field = value;
                OnPropertyChanged(propertyName);

                return true;
            }

            public string DisplayName { get; private set; }
            public string Icon { get; private set; }
            public Type Type { get; private set; }

            private bool _isSelected = false;
            public bool IsSelected { get { return _isSelected; } set { SetField(ref _isSelected, value); } }

            public InheritorInfo(string DisplayName, string Icon, Type Type)
            {
                this.DisplayName = DisplayName;
                this.Icon = Icon;
                this.Type = Type;
            }
        }

        public static InheritorInfo[] List {

            get {

                return Assembly.GetExecutingAssembly().DefinedTypes.Where(info =>
                {
                    Type t = info;
                    
                    if ( t.BaseType == null || !t.IsSubclassOf(typeof(VisualizationControl)) )
                        return false;

                    if (t.ContainsGenericParameters)
                    {
                        t = t.MakeGenericType(typeof(object));
                    }

                    PropertyInfo isVisible = t.GetProperty("isVisible", BindingFlags.Public | BindingFlags.Static);

                    return isVisible == null || (bool)isVisible.GetValue(null);

                })
                .Select(info => {

                    Type t = info;

                    if (info.ContainsGenericParameters)
                    {
                        t = info.MakeGenericType(typeof(object));
                    }

                    PropertyInfo DisplayName = t.GetProperty("DisplayName", BindingFlags.Public | BindingFlags.Static);
                    PropertyInfo Icon        = t.GetProperty("Icon", BindingFlags.Public);
                    
                    return new InheritorInfo(
                        DisplayName == null ? "" : (string)DisplayName.GetValue(null),
                        Icon == null ? "" : (string)Icon.GetValue(null),
                        info
                    );

                }).ToArray();
            }
        }

        #endregion
        
        public Guid Id { get; protected set; } = Guid.NewGuid();
        public virtual string Title { get; set; }
        
        public VisualizationControl()
        {
            this.DataContext = this;
        }

        ~VisualizationControl()
        {
            Dispose();
        }

        public virtual void Refresh()
        {

        }

        public virtual void Dispose()
        {
            foreach(var item in controls.ToArray())
            {
                Remove(item.Key);
            }

            controls.Clear();

            GC.SuppressFinalize(this);
        }

        public Controls GetControlsFor(KeyItem key)
        {
            return (from s in controls where s.Key.Path == key.Path select s.Value).ElementAtOrDefault(0);
        }
        
        private ObservableCollection<KeyValuePair<KeyItem, Controls>> controls = new ObservableCollection<KeyValuePair<KeyItem, Controls>>();
        public ObservableCollection<KeyValuePair<KeyItem, Controls>> Controls
        {
            get
            {
                return controls;
            }
        }

        private TimeSpan Timespan { get; set; } = new TimeSpan(365, 0, 0, 0, 0);

        protected virtual Controls CreateControl(KeyItem key)
        {
            return new Controls<DateTime, TimeSpan>(new ObservableCollection<EntryItem>(), DateTime.Now, Timespan) { Selected = key };
        }
        
        public virtual void Add(KeyItem key, ObservableCollection<EntryItem> collection = null)
        {

            if (key.Type == null)
            {

                key.Children.CollectionChanged += Children_CollectionChanged;
                Dispatcher.InvokeAsync(key.Reload);

                return;
            }

            Controls control = GetControlsFor(key);

            if (control == null)
            {
                control = CreateControl(key);

                if (collection != null)
                    control.Entries = collection;

                controls.Add(new KeyValuePair<KeyItem, Controls>(key, control));
            }

        }

        public virtual void Remove(KeyItem key)
        {

            key.Children.CollectionChanged -= Children_CollectionChanged;

            foreach(var item in (from s in Controls.ToArray() where s.Key.Path == key.Path select s))
            {
                Controls.Remove(item);
                item.Value.Dispose();
            }

        }

        protected void Children_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {

            foreach (Controls c in (from s in controls select s.Value))
            {
                if (e.OldItems != null && (from KeyItem k in e.OldItems where k.Path == c.Selected.Path select k).ElementAtOrDefault(0) == null)
                    Remove(c.Selected);
            }

            if (e.NewItems == null) return;

            foreach (KeyItem k in e.NewItems)
            {
                if (k.Type != null && (from s in controls where s.Key.Path == k.Path select s.Value).ElementAtOrDefault(0) == null)
                    Add(k);
            }

        }

        public virtual void Reload()
        {

            DateTime now = DateTime.Now;

            foreach (KeyValuePair<KeyItem, Controls> pair in (from p in controls select p))
            {

                Controls control = pair.Value;
                KeyItem key = pair.Key;

                control.Start = now.Subtract(Timespan);

                key.Reload();
                control.Reload();

                Refresh();

            }

        }
        
    }
}
