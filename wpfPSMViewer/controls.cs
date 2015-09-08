﻿/// <copyright file="controls.cs" company="Baker Hughes Incorporated">
/// Copyright (c) 2015 All Rights Reserved
/// </copyright>
/// <author>Odd Marthon Lende</author>
/// <summary>Controls used when loading data from the <see cref="PSM.Store"/></summary>
/// 

using PSMonitor;
using PSMViewer.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace PSMViewer.ViewModels
{


    /// <summary>
    /// The handler that will be called when a reload request is made to update the data
    /// </summary>
    /// <param name="reloadable">The object that implements <see cref="IReload"/> interface</param>
    public delegate void LoadHandler(IReload reloadable);
    /// <summary>
    /// Event handler that is called when the controls are activated
    /// </summary>
    /// <param name="sender">The <see cref="Controls"/></param>
    public delegate void RequestedActivationEventHandler(Controls sender);
    /// <summary>
    /// Handles DataChanged events
    /// </summary>
    /// <param name="sender"></param>
    public delegate void DataChangedEventHandler(object sender);

    /// <summary>
    /// A base class for data controls
    /// </summary>
    public abstract class Controls : DispatcherObject, IReload, IDisposable, INotifyPropertyChanged
    {

        private ReloadStatus _status = ReloadStatus.Idle;
        /// <summary>
        /// <see cref="IReload.Status"/>
        /// </summary>
        public virtual ReloadStatus Status {

            get {
                return _status;
            }

            set
            {
                SetField(ref _status, value);
            }

        }

        /// <summary>
        /// <see cref="IReload.Cancel"/>
        /// </summary>
        public CancellationTokenSource Cancel { get; protected set; } = new CancellationTokenSource();

        /// <summary>
        /// Hold references to all controls that has been instantiated.
        /// </summary>
        private static List<KeyValuePair<object, Controls>> Instances = new List<KeyValuePair<object, Controls>>();

        /// <summary>
        /// The ActivationRequested will be emitted when a controls <see cref="Controls.Activate"/> method is called.
        /// </summary>
        public event RequestedActivationEventHandler ActivationRequested;
        /// <summary>
        /// <see cref="INotifyPropertyChanged.PropertyChanged"/>
        /// </summary>
        public event PropertyChangedEventHandler     PropertyChanged;
        /// <summary>
        /// The DataChanged event will be emitted when the data is updated
        /// </summary>
        public event DataChangedEventHandler         DataChanged;

        /// <summary>
        /// The handler that will call the <see cref="IReload.Reload"/> method and set the <see cref="IReload.Status"/>
        /// </summary>
        public LoadHandler Load
        {
            get; set;
        } = null;

        private bool _active = false;
        /// <summary>
        /// Is this control active? 
        /// </summary>
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

        private ObservableCollection<EntryItem> _entries = new ObservableCollection<EntryItem>();
        /// <summary>
        /// A reference to the collection that holds the data.
        /// </summary>
        public ObservableCollection<EntryItem> Entries
        {
            get
            {
                return _entries;
            }
            set
            {
                if (_entries != null)
                {
                    _entries.CollectionChanged -= OnDataChanged;
                }

                _entries = value;

                if (_entries != null)
                    _entries.CollectionChanged += OnDataChanged;
            }
        }

        /// <summary>
        /// The constructor
        /// </summary>
        public Controls()
        { }

        /// <summary>
        /// A constructor that takes a <see cref="LoadHandler"/> as an argument
        /// </summary>
        /// <param name="Handler">The handler</param>
        public Controls(LoadHandler Handler) : this()
        {
            Load += Handler;
        }

        /// <summary>
        /// Triggers the <see cref="Load"/> handler
        /// </summary>
        /// <param name="controls"></param>
        protected void OnReload(Controls controls)
        {
            if (Load != null)
                Load(controls);
        }
                
        /// <summary>
        /// Append a data entry
        /// </summary>
        /// <param name="item"></param>
        protected virtual void Append(EntryItem item) {

            if (!IsActive) return;
            Entries.Insert(0, item);
        }

        /// <summary>
        /// Destructor/Finalize method
        /// </summary>
        ~Controls()
        {
            Dispose();
        }

        /// <summary>
        /// <see cref="IDisposable.Dispose"/>
        /// </summary>
        public virtual void Dispose()
        {

            Instances.TakeWhile((pair) =>
            {
                return pair.Value == this;
            });
                        
            if (_entries != null)
                _entries.CollectionChanged -= OnDataChanged;

            this.IsActive = false;

            GC.SuppressFinalize(this);
        }

        #region Data Changed Eventemitter        
        
        private DispatcherOperation OnDataChangedDispatcherOperation = null;
        /// <summary>
        /// Invoked by the <see cref="IReload.Dispatcher"/>, when the data is updated.
        /// </summary>
        private void InvokeDataChanged ()
        {
            DataChangedEventHandler handler = DataChanged;

            if(handler != null)
                handler(this);

            OnDataChangedDispatcherOperation = null;
        }

        /// <summary>
        /// Triggers the datachanged event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void OnDataChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if ( OnDataChangedDispatcherOperation == null)
                OnDataChangedDispatcherOperation = Dispatcher.InvokeAsync(InvokeDataChanged);
        }

        #endregion

        /// <summary>
        /// Triggers the <see cref="INotifyPropertyChanged.PropertyChanged"/> event
        /// </summary>
        /// <param name="propertyName"></param>
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
        /// <summary>
        /// The currently selected <see cref="KeyItem"/> that this control handles the data for.
        /// </summary>
        public virtual KeyItem Selected
        {
            get {
                return _selected;
            }
            set
            {
                PSM.Store.Unregister(this);
                SetField<KeyItem>(ref _selected, value);
            }
        }

        private object _start = null;
        /// <summary>
        /// The starting index from which to start to load data.
        /// </summary>
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
        /// <summary>
        /// The end index from which to end the loading of data.
        /// </summary>
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
        /// <summary>
        /// A length that describes the data interval
        /// </summary>
        public virtual object Count
        {
            get { return _count; }
            set
            {
                SetField(ref _count, value);
            }
        }

        /// <summary>
        /// Reload data for the specified key using the <see cref="Start"/> and <see cref="End"/> properties.
        /// </summary>
        /// <param name="key">The key</param>
        /// <returns>The data</returns>
        public abstract IEnumerable<EntryItem> Reload(KeyItem key);

        /// <summary>
        /// Reloads the data.
        /// </summary>
        public abstract void Reload();

        /// <summary>
        /// Move to the next result set.
        /// </summary>
        /// <returns></returns>
        public abstract bool Next();

        /// <summary>
        /// Move to the next result set.
        /// </summary>
        /// <returns></returns>
        public abstract bool Previous();

        public virtual void Stop()
        {
            PSM.Store.Unregister(this);
        }

        /// <summary>
        /// Triggers the <see cref="ActivationRequested"/> event.
        /// </summary>
        protected virtual void OnActivationRequested()
        {
            if (ActivationRequested != null)
                ActivationRequested(this);
        }

       /// <summary>
       /// Set this control's <see cref="IsActive"/> property to <c>true</c> and other controls with the same <paramref name="context"/> to <c>false</c>
       /// </summary>
       /// <param name="context">An object that identifies which context this control belongs to.</param>
        public virtual void Activate(object context) {


            if (context == null)
                throw new ArgumentNullException("context cannot be null");

            KeyValuePair<object, Controls> p = new KeyValuePair<object, Controls>(context, this);

            if (!Instances.Contains(p))
                Instances.Add(p);

            foreach (KeyValuePair<object, Controls> pair in Instances) {               

                if (pair.Key != context)
                    continue;

                pair.Value.IsActive = pair.Value == this;

            }

            OnActivationRequested();
        }

        
    }

    /// <summary>
    /// A generic class for data controls
    /// </summary>
    /// <typeparam name="T">DateTime or a number type</typeparam>
    /// <typeparam name="TCount">Timespan or a number type</typeparam>
    public class Controls<T, TCount> : Controls, INotifyPropertyChanged
    {

        private string _typeName = typeof(T).Name.ToLower();

        private Task<IEnumerable<EntryItem>> ReloadTask = null;

        private Dictionary<string, Queue<Entry>> Data = new Dictionary<string, Queue<Entry>>();

        private DispatcherOperation ProcessQueueOperation = null;

        private ReloadStatus _status = ReloadStatus.Idle;
        /// <summary>
        /// <see cref="Controls.Status"/>
        /// </summary>
        public override ReloadStatus Status
        {
            get
            {
                return ReloadTask != null ? _status : base.Status;
            }

            set
            {
                base.Status = value;
            }
        }

        private object _start = null;
        /// <summary>
        /// <see cref="Controls.Start"/>
        /// </summary>
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
        
        /// <summary>
        /// <see cref="Controls.End"/>
        /// </summary>
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

        /// <summary>
        /// <see cref="Controls.Count"/>
        /// </summary>
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

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="Entries"></param>
        /// <param name="Start"></param>
        /// <param name="Count"></param>
        public Controls(ObservableCollection<EntryItem> Entries, object Start, object Count)
        {
            this.Entries = Entries??this.Entries;
            this.Start = Start;
            this.Count = Count;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="other"></param>
        public Controls(Controls<T, TCount> other) : this(other.Entries, other.Start, other.Count)
        {
            Selected = other.Selected;
        }

        /// <summary>
        /// <see cref="Controls.Reload(KeyItem)"/>
        /// </summary>
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

        /// <summary>
        /// <see cref="Controls.Append(EntryItem)"/>
        /// </summary>
        protected override void Append(EntryItem item)
        {

            switch (_typeName)
            {

                case "datetime":

                    DateTime start = (DateTime)Start;
                    DateTime end = (DateTime)End;
                    DateTime ts = item.Timestamp;

                    if (ts >= start && ts <= end)
                        break;
                    else
                        return;

                case "byte":
                case "int16":
                case "int32":
                case "int64":
                    {

                        if ((long)Start > 0) return;                        
                        break;

                    }

            }

            base.Append(item);
        }        

        /// <summary>
        /// <see cref="Controls.Reload"/>
        /// </summary>
        public override async void Reload()
        {

            IEnumerable<EntryItem> data = null;

            PSM.Store.Unregister(this);

            if (ReloadTask != null)
            {
                Cancel.Cancel();
                Cancel = new CancellationTokenSource();
            }                

            try {

                ReloadTask = Task.Factory.StartNew<IEnumerable<EntryItem>>(delegate
                {

                    while (true)
                    {
                        
                        try
                        {
                            return Reload(Selected);
                        }
                        catch (Exception error) {

                            Dispatcher.Invoke(delegate
                            {
                                SetField(ref _status, ReloadStatus.Error, "Status");
                            });
                            
                            Logger.error(error);

                        }

                    }

                }, Cancel.Token);

                await Dispatcher.InvokeAsync(delegate
                {
                    SetField(ref _status, ReloadStatus.Loading, "Status");
                });

                data = await ReloadTask;

            }
            catch(TaskCanceledException)
            {
                return;
            }       

            Entries.Clear();
            
            foreach (EntryItem entry in data ?? Entries)
            {
                Entries.Add(entry);
            }

            DateTime StartReceiveIndex = Entries.Count > 0 ? Entries.Max((item) =>
            {
                return item.Timestamp;
            }) : DateTime.Now;            

            SetField(ref _status, ReloadStatus.Idle, "Status");
            ReloadTask = null;

            Debug.WriteLine("Loading... {0}", Selected);

            Dispatcher.InvokeAsync(delegate
            {

                Debug.WriteLine("Registering... {0}", Selected);
                PSM.Store.Register(this, Selected.ToString(), StartReceiveIndex, Store_DataReceived);

            }, DispatcherPriority.ContextIdle);

        }        

        /// <summary>
        /// Handles the receival of new data that is added to the store after the last reload
        /// </summary>
        /// <param name="data">The data envelope that was received</param>
        /// <return>The highest timestamp in the dataset.</return>
        private object Store_DataReceived(Envelope data)
        {

            if (IsActive)
            {

                if (!Data.ContainsKey(data.Path))
                    Data.Add(data.Path, new Queue<Entry>());

                Queue<Entry> queue;

                Data.TryGetValue(data.Path, out queue);

                foreach (Entry entry in data.Entries)
                {
                    queue.Enqueue(entry);
                }

                if(ProcessQueueOperation == null)
                    ProcessQueueOperation = Dispatcher.InvokeAsync(ProcessQueue, DispatcherPriority.Normal);

            }

            return data.Entries.Max(entry => { return entry.Timestamp; });
        }        

        /// <summary>
        /// Processes the queue and updates data
        /// </summary>
        private void ProcessQueue()
        {          

            Queue<Entry> queue;

            ProcessQueueOperation = null;

            if (!Data.TryGetValue(Selected.Parent.Path, out queue))
                return;            

            foreach (Entry entry in queue)
            {
                if (entry.Key == Selected.Name)
                    Append((EntryItem)entry);
            }

            queue.Clear();

            switch (_typeName)
            {

                case "datetime" :

                    DateTime start = (DateTime)Start;
                    DateTime end = (DateTime)End;
                    
                    EntryItem entry = null;

                    int count = Entries.Count;

                    while(count > 0)
                    {

                        entry = Entries.First();

                        if(entry.Timestamp > end)
                        {
                            Entries.RemoveAt(0);
                            count--;

                            continue;
                        }

                        break;
                    }

                    count = Entries.Count;

                    while (count > 0)
                    {

                        entry = Entries.Last();

                        if (entry.Timestamp < start)
                        {
                            Entries.Remove(entry);
                            count--;

                            continue;
                        }

                        break;
                    }                                    

                    break;

                case "byte"  :
                case "int16" :
                case "int32" :
                case "int64" :
                {

                        while (Entries.Count > (long)Count)
                            Entries.RemoveAt(Entries.Count - 1);

                        break;

                }

            }
            
            
        }

        /// <summary>
        /// Move to the next result set
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Move to the previous result set
        /// </summary>
        /// <returns></returns>
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