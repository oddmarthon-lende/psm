using PSMonitor;
using PSMViewer.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace PSMViewer.ViewModels
{

    public delegate void RequestedActivationEventHandler(Controls sender);
    public delegate void DataChangedEventHandler(object sender);

    public abstract class Controls : IReload, IDisposable, INotifyPropertyChanged
    {

        private static List<Controls> Instances = new List<Controls>();

        public event RequestedActivationEventHandler ActivationRequested;
        public event PropertyChangedEventHandler     PropertyChanged;
        public event DataChangedEventHandler         DataChanged;

        public Controls()
        {
            Instances.Add(this);
        }

        ~Controls()
        {
            Dispose();
        }

        public virtual void Dispose()
        {

            Instances.Remove(this);

            if(entries != null)
                entries.CollectionChanged -= OnDataChanged;

            GC.SuppressFinalize(this);
        }

        #region Data Changed Eventemitter
        
        private DispatcherOperation OnDataChangedDispatcherOperation = null;

        private void InvokeDataChanged ()
        {
            DataChangedEventHandler handler = DataChanged;

            if(handler != null)
                handler(this);

            OnDataChangedDispatcherOperation = null;
        }

        protected virtual void OnDataChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            Dispatcher Dispatcher = Dispatcher.CurrentDispatcher;

            if ( OnDataChangedDispatcherOperation == null)
                OnDataChangedDispatcherOperation = Dispatcher.InvokeAsync(InvokeDataChanged);

        }
        #endregion

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

        private KeyItem _selected = null;
        public virtual KeyItem Selected
        {
            get { return _selected; }
            set
            {
                SetField<KeyItem>(ref _selected, value);
            }
        }

        private object _start = null;
        public virtual object Start
        {
            get
            {
                return _start;
            }
            set
            {
                SetField(ref _start, value);
            }
        }

        private object _end = null;
        public virtual object End {
            get
            {
                return _end;
            }
            set {
                SetField(ref _end, value);
            }
        }

        private object _count = null;
        public virtual object Count
        {
            get { return _count; }
            set
            {
                SetField(ref _count, value);
            }
        }

        public abstract IEnumerable<EntryItem> Reload(KeyItem key);
        public abstract void Reload();
        public abstract bool Next();
        public abstract bool Previous();

        public virtual void Activate() {

            Instances.ForEach(c => { c.isActive = c == this; });

            if (ActivationRequested != null)
                ActivationRequested(this);

        }

        private bool _active = false;
        public virtual bool isActive
        {
            get
            {
                return _active;
            }
            set
            {
                SetField(ref _active, value);
            }
        }

        private ObservableCollection<EntryItem> entries = new ObservableCollection<EntryItem>();
        public ObservableCollection<EntryItem> Entries
        {
            get
            {
                return entries;
            }
            set
            {
                if(entries != null)
                {
                    entries.CollectionChanged -= OnDataChanged;
                }

                entries = value;

                if(entries != null)
                    entries.CollectionChanged += OnDataChanged;
            }
        }
    }

    public class Controls<T, TCount> : Controls, INotifyPropertyChanged
    {

        private T _start;
        public override object Start
        {
            get
            {
                return _start;
            }

            set
            {

                if (SetField<T>(ref _start, (T)Convert.ChangeType(value, typeof(T))))
                    OnPropertyChanged("End");

            }
        }

        public override object End
        {

            get
            {

                switch (typeof(T).Name.ToLower())
                {

                    case "datetime":
                        return (T)(object)((DateTime)(object)Start + (TimeSpan)(object)Count);

                    case "int64":
                        return (T)(object)((long)(object)Start + (long)(object)Count - 1L);

                }

                return default(T);

            }

            set
            {
                
                switch (typeof(T).Name.ToLower())
                {

                    case "datetime":

                        switch(typeof(TCount).Name.ToLower())
                        {
                            case "timespan":
                                Count = ((DateTime)value) - ((DateTime)Start);
                                break;
                        }
                        break;               

                    case "int64":

                        switch (typeof(TCount).Name.ToLower())
                        {
                            case "int64":
                                Count = (long)value - (long)Start;
                                break;
                        }
                        break;

                }                
            }
        }

        private TCount _count;
        public override object Count
        {
            get { return _count; }
            set
            {

                if (SetField<TCount>(ref _count, (TCount)Convert.ChangeType(value, typeof(TCount))))
                {
                    OnPropertyChanged("End");
                }

            }
        }

        public Controls(ObservableCollection<EntryItem> Entries, object Start, object Count)
        {
            this.Entries = Entries??this.Entries;
            this.Start = Start;
            this.Count = Count;
        }

        public override IEnumerable<EntryItem> Reload(KeyItem key)
        {
            IEnumerable<Entry> enumerable = null;

            if (key != null)
            {

                switch (typeof(T).Name.ToLower())
                {

                    case "datetime":

                        enumerable = (PSM.Store.Get(key.Path, (DateTime)Start, (DateTime)End));
                        break;

                    case "byte":
                    case "int16":
                    case "int32":
                    case "int64":

                        enumerable = (PSM.Store.Get(key.Path, (long)Start, (long)End));
                        break;
                }
            }

            return enumerable == null ? null : enumerable.Select(entry => {
                return (EntryItem)entry;
            });
        }

        public override void Reload()
        {

            Entries.Clear();

            foreach (EntryItem entry in Reload(Selected)??Entries)
            {
                Entries.Add(entry);
            }
        }

        public override bool Next()
        {

            switch (typeof(T).Name.ToLower())
            {
                case "datetime":

                    Start = ((DateTime)Start + (TimeSpan)Count);
                    break;

                case "byte":
                case "int16":
                case "int32":
                case "int64":

                    if (Entries.Count < (long)Convert.ChangeType(Count, typeof(long))) return false;

                    Start = ((long)Start + (long)Count);
                    break;

                default:
                    return false;
            }

            Reload();

            return true;

        }

        public override bool Previous()
        {

            switch (typeof(T).Name.ToLower())
            {
                case "datetime":

                    Start = ((DateTime)Start - (TimeSpan)Count);
                    break;

                case "byte":
                case "int16":
                case "int32":
                case "int64":

                    if ((long)Start == 0L) return false;

                    Start = Math.Max(0L, (long)Start - (long)Count);
                    break;

                default:
                    return false;
            }

            Reload();

            return true;

        }

    }
}
