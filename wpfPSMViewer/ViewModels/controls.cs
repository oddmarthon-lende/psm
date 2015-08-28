/// <copyright file="controls.cs" company="Baker Hughes Incorporated">
/// Copyright (c) 2015 All Rights Reserved
/// </copyright>
/// <author>Odd Marthon Lende</author>
/// <summary>Controls used when loading data from the <see cref="PSM.Store"/></summary>
/// 

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


    public delegate void LoadEventHandler(IReload reloadable);
    public delegate void RequestedActivationEventHandler(Controls sender);
    public delegate void DataChangedEventHandler(object sender);

    /// <summary>
    /// A base class for data controls
    /// </summary>
    public abstract class Controls : DispatcherObject, IReload, IDisposable, INotifyPropertyChanged
    {

        public ReloadStatus Status { get; set; } = ReloadStatus.Idle;

        private CancellationTokenSource _c = new CancellationTokenSource();
        public CancellationTokenSource Cancel
        {
            get
            {
                return _c;
            }
        }

        private static List<Controls> Instances = new List<Controls>();

        public event RequestedActivationEventHandler ActivationRequested;
        public event PropertyChangedEventHandler     PropertyChanged;
        public event DataChangedEventHandler         DataChanged;
        public event LoadEventHandler                Load;
        
        public Controls()
        {

            Instances.Add(this);

            PSM.Store.DataReceived += Store_DataReceived;

            Dispatcher.Hooks.OperationStarted += delegate
            {
                Status = ReloadStatus.Loading;
            };

            Dispatcher.Hooks.OperationCompleted += delegate
            {
                Status = ReloadStatus.Idle;
            };

            Dispatcher.UnhandledException += delegate
            {
                Status = ReloadStatus.Error;
            };
        }

        public Controls(LoadEventHandler Handler) : this()
        {
            Load += Handler;
        }

        protected void OnReload(Controls controls)
        {
            if (Load != null)
                Load(controls);
        }

        private void Store_DataReceived(Envelope data)
        {
            if(Dispatcher.Thread != Thread.CurrentThread)
            {
                Dispatcher.Invoke(() => Store_DataReceived(data));

                return;
            }

            if(this.Selected != null && this.Selected.Parent != null && this.Selected.Parent.Path == data.Path)
            {
                foreach (Entry entry in data.Entries)
                {
                    if(this.Selected.Name == entry.Key)
                        Append((EntryItem)entry);
                }
            }
        }

        protected virtual void Append(EntryItem item) {

            if (!IsActive) return;
            
            Entries.Insert(0, item);  
                      
            OnDataChanged(null, null);
        }

        ~Controls()
        {
            Dispose();
        }

        public virtual void Dispose()
        {

            Instances.Remove(this);

            PSM.Store.DataReceived -= Store_DataReceived;

            if (entries != null)
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

            Instances.ForEach(c => { c.IsActive = c == this; });

            if (ActivationRequested != null)
                ActivationRequested(this);

        }

        private bool _active = false;
        public virtual bool IsActive
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

    /// <summary>
    /// A generic class for data controls
    /// </summary>
    /// <typeparam name="T">DateTime or a number type</typeparam>
    /// <typeparam name="TCount">Timespan or a number type</typeparam>
    public class Controls<T, TCount> : Controls, INotifyPropertyChanged
    {

        private object _start = null;
        public override object Start
        {
            get
            {                
                object default_value = null;

                if (_start != null) return _start;

                switch (typeof(T).Name.ToLower())
                {

                    case "datetime":

                        switch (typeof(TCount).Name.ToLower())
                        {

                            case "timespan" :

                                default_value = (DateTime.Now - (TimeSpan)Count);
                                break;
                        }

                        break;

                    case "byte":
                    case "int16":
                    case "int32":
                    case "int64":

                        default_value = 0D;
                        break;

                }

                return default_value;
            }

            set
            {
                
                if (SetField(ref _start, value == null ? null : Convert.ChangeType(value, typeof(T))))
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

                        switch (typeof(TCount).Name.ToLower())
                        {
                            case "timespan":

                                return ((DateTime)Start + (TimeSpan)Count);
                        }
                        break;                        

                    case "byte":
                    case "int16":
                    case "int32":
                    case "int64":

                        return (object)((long)Start + (long)Count - 1L);

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

                    case "byte":
                    case "int16":
                    case "int32":
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

        private object _count = default(TCount);
        public override object Count
        {
            get { return _count; }
            set
            {

                if (SetField(ref _count, Convert.ChangeType(value, typeof(TCount))))
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

        public Controls(Controls<T, TCount> other) : this(other.Entries, other.Start, other.Count)
        {
            Selected = other.Selected;
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

        protected override void Append(EntryItem item)
        {

            switch (typeof(T).Name.ToLower())
            {

                case "datetime":

                    if (item.Timestamp >= (DateTime)Start && item.Timestamp <= (DateTime)End)
                        break;
                    else
                        return;

                case "byte":
                case "int16":
                case "int32":
                case "int64":
                    {

                        if ((long)Start > 0) return;

                        while (Entries.Count >= (long)Count)
                            Entries.RemoveAt(Entries.Count - 1);

                        break;

                    }

            }

            base.Append(item);
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

            OnReload(this);

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

            OnReload(this);

            return true;

        }

    }
}
