﻿/// <copyright file="controls.cs" company="Baker Hughes Incorporated">
/// Copyright (c) 2015 All Rights Reserved
/// </copyright>
/// <author>Odd Marthon Lende</author>
/// <summary>Controls used when loading data from the <see cref="PSM.Store"/></summary>
/// 

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace PSM.Viewer.Models
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
        /// <see cref="IReload.CancellationTokenSource"/>
        /// </summary>
        public CancellationTokenSource CancellationTokenSource { get; set; } = new CancellationTokenSource();

        /// <summary>
        /// Hold references to all controls that has been instantiated.
        /// </summary>
        private static ConcurrentBag<KeyValuePair<object, Controls>> Instances = new ConcurrentBag<KeyValuePair<object, Controls>>();

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

        private ObservableCollection<Entry> _entries = new ObservableCollection<Entry>();
        /// <summary>
        /// A reference to the collection that holds the data.
        /// </summary>
        public ObservableCollection<Entry> Entries
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

        protected int _page = 0;
        public int Page
        {
            get
            {
                return _page;
            }

            set
            {
                SetField(ref _page, value, "Page");
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
        protected virtual void Append(Entry item)
        {
            
            if (!IsActive) return;
            Entries.Insert(0, item);
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

            Unregister();

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
                SetField<KeyItem>(ref _selected, value);
            }
        }

        private IComparable _start = null;
        /// <summary>
        /// The starting index from which to start to load data.
        /// </summary>
        public virtual IComparable Start
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

        private IComparable _end = null;
        /// <summary>
        /// The end index from which to end the loading of data.
        /// </summary>
        public virtual IComparable End {
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
        public abstract IEnumerable<Entry> Reload(KeyItem key);

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

        public abstract void Register();

        public virtual void Unregister()
        {
            Store.Get(Dispatcher).Unregister(this);
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

        public virtual void Reset()
        {
            Page = 0;
            OnPropertyChanged("Start");
            OnPropertyChanged("End");
        }

        
    }

    /// <summary>
    /// A generic class for data controls
    /// </summary>
    /// <typeparam name="T">DateTime or a number type</typeparam>
    /// <typeparam name="TCount">Timespan or a number type</typeparam>
    public class Controls<T, TCount> : Controls, INotifyPropertyChanged
        where T : IComparable
        where TCount : IComparable
    {

        
        /// <summary>
        /// 
        /// </summary>
        private Task<IEnumerable<Entry>> ReloadTask = null;

        /// <summary>
        /// 
        /// </summary>
        private static ConcurrentDictionary<Controls, ConcurrentQueue<Entry>> _data = new ConcurrentDictionary<Controls, ConcurrentQueue<Entry>>();

        /// <summary>
        /// 
        /// </summary>
        private DispatcherOperation ProcessQueueOperation = null;

        /// <summary>
        /// 
        /// </summary>
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

        private IComparable _start = null;

        private object _count = default(TCount);

        /// <summary>
        /// <see cref="Controls.Start"/>
        /// </summary>
        public override IComparable Start
        {
            get
            {
                IComparable default_value = null;

                if (_start != null)
                    return _start;

                switch (typeof(T).Name.ToLower())
                {

                    case "datetime":

                        switch (typeof(TCount).Name.ToLower())
                        {

                            case "timespan" :

                                {
                                    TimeSpan ts = (TimeSpan)Count;

                                    for(int i = 0; i < Page; i++)
                                    {
                                        ts += (TimeSpan)Count;
                                    }

                                    default_value = (DateTime.Now - ts);
                                }

                                break;
                        }

                        break;

                    case "byte":
                    case "int16":
                    case "int32":
                    case "int64":

                        default_value = (long)Count * Page;

                        break;

                }

                return default_value;
            }

            set
            {
                
                if (SetField(ref _start, value == null ? null : (IComparable)Convert.ChangeType(value, typeof(T))))
                    OnPropertyChanged("End");

            }
        }
        
        /// <summary>
        /// <see cref="Controls.End"/>
        /// </summary>
        public override IComparable End
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

                        return (IComparable)((long)Start + (long)Count - 1L);

                }

                return (IComparable)default(T);

            }

            set
            {
                long result = 0;

                switch(TypeInfo.GetTypeCode(value.GetType()))
                {
                    case TypeCode.String:

                        if (String.IsNullOrEmpty((string)value))
                            return;

                        try {
                            value = Convert.ToInt64(value);
                        }
                        catch(Exception)
                        {
                            return;
                        }

                        break;
                }
                
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

                                result = (long)value - (long)Start;

                                if (result < 0) return;

                                Count = result + 1;

                                break;
                        }

                        break;

                }                
            }
        }        

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
        public Controls(ObservableCollection<Entry> Entries, object Count)
        {
            this.Entries = Entries??this.Entries;
            this.Count = Count;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="other"></param>
        public Controls(Controls<T, TCount> other) : this(other.Entries, other.Count)
        {
            Selected = other.Selected;
        }

        /// <summary>
        /// <see cref="Controls.Reload(KeyItem)"/>
        /// </summary>
        public override IEnumerable<Entry> Reload(KeyItem key)
        {

            IEnumerable<Entry> enumerable = null;

            if (key != null)
            {
                enumerable = (Store.Get(Dispatcher).Read(key.Path, Start, End, key.IndexIdentifier));
            }

            return enumerable == null ? null : enumerable.Select(entry => {
                return entry;
            });
        }        

        /// <summary>
        /// <see cref="Controls.Append(Entry)"/>
        /// </summary>
        protected override void Append(Entry item)
        {

            string IndexTypeName = item.Index[Selected.IndexIdentifier].GetType().Name.ToLower();

            switch (IndexTypeName)
            {
                
                case "datetime":

                    DateTime start = (DateTime)Start;
                    DateTime end = (DateTime)End;
                    DateTime ts = (DateTime)item.Index[Selected.IndexIdentifier];

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

            switch (IndexTypeName)
            {

                case "datetime":

                    DateTime start = (DateTime)Start;
                    DateTime end = (DateTime)End;

                    Entry entry = null;

                    int count = Entries.Count;

                    while (count > 0)
                    {

                        entry = Entries.First();

                        if (entry.Index[Selected.IndexIdentifier].CompareTo(end) > 0)
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

                        if (entry.Index[Selected.IndexIdentifier].CompareTo(start) < 0)
                        {
                            Entries.Remove(entry);
                            count--;

                            continue;
                        }

                        break;
                    }

                    break;

                case "byte":
                case "int16":
                case "int32":
                case "int64":
                    {

                        while (Entries.Count > (long)Count)
                            Entries.RemoveAt(Entries.Count - 1);

                        break;

                    }

            }

            
        }
        
        private static void ProcessReload(object ctx)
        {

            Controls ctrl = (Controls)ctx;
            Entry[] entries;

            try
            {
                Debug.WriteLine("Loading... {0}", ctrl.Selected);

                entries = ctrl.Reload(ctrl.Selected).ToArray();

                ctrl.Dispatcher.InvokeAsync(delegate
                {
                    ctrl.Entries.Clear();

                    foreach (Entry entry in entries)
                    {
                        ctrl.Entries.Add(entry);
                    }

                    if (ctrl.Page == 0)
                        ctrl.Register();

                    ctrl.Status = ReloadStatus.Idle;

                }, DispatcherPriority.Send, ctrl.CancellationTokenSource.Token);


            }
            catch (ThreadAbortException)
            {
                Debug.WriteLine(String.Format("Thread \"{0}\" was aborted ", Thread.CurrentThread.Name));
            }
            catch (Exception error)
            {

                ctrl.Dispatcher.InvokeAsync(delegate
                {
                    ctrl.Status = ReloadStatus.Error;
                });

                Logger.Error(error);
            }
                                   

        }

        private Thread _thread;

        /// <summary>
        /// <see cref="Controls.Reload"/>
        /// </summary>
        public override void Reload()
        {

            if(_thread != null)
            {
                if (!_thread.IsAlive)
                    _thread = null;
                else
                {
                    _thread.Abort();
                }
            }
                                    
            Unregister();
            
            Status = ReloadStatus.Loading;
            
            _thread = new Thread((ParameterizedThreadStart)ProcessReload) { Name = "Reload - " + base.Selected.Path };
            _thread.Start(this);

        }

        public override void Register()
        {

            IComparable startIndex = null;

            if (Selected == null)
                return;

            if (Entries.Count > 0)
                startIndex = Entries.Max(entry => { return entry.Index[Selected.IndexIdentifier]; });
            else
            {

                startIndex = (IComparable)default(T);

                if (startIndex is DateTime)
                    startIndex = DateTime.Now;

            }            

            Debug.WriteLine("Registering... {0}", Selected);

            Store.Get(Dispatcher).Register(this, Selected.ToString(), startIndex, Selected.IndexIdentifier, Received);

        }

        /// <summary>
        /// Handles the receival of new data that is added to the store after the last reload
        /// </summary>
        /// <param name="data">The data envelope that was received</param>
        /// <return>The highest timestamp in the dataset.</return>
        private Index Received(Envelope data)
        {

            Index startIndex = null;

            if (IsActive)
            {
                
                if (!_data.ContainsKey(this))
                    while(!_data.TryAdd(this, new ConcurrentQueue<Entry>()));

                ConcurrentQueue<Entry> queue;

                while(!_data.TryGetValue(this, out queue));

                foreach (Entry entry in data.Entries.OrderBy( (e) => { return e.Index[Selected.IndexIdentifier]; }))
                {
                    if (startIndex == null || entry.Index[Selected.IndexIdentifier].CompareTo(startIndex[Selected.IndexIdentifier]) > 0)
                        startIndex = entry.Index;

                    queue.Enqueue(entry);
                }

                if(ProcessQueueOperation == null)
                    ProcessQueueOperation = Dispatcher.InvokeAsync(ProcessQueue);

            }
            
            return startIndex;

        }

       
        /// <summary>
        /// Processes the queue and updates data
        /// </summary>
        private void ProcessQueue()
        {

            ConcurrentQueue<Entry> queue;

            while (!_data.TryGetValue(this, out queue)) ;

            while (queue.Count > 0)
            {
                Entry entry;

                while (!queue.TryDequeue(out entry)) ;

                if (entry.Key == Selected.Name)
                    Append((Entry)entry);
            }

            ProcessQueueOperation = null;

        }
        
        /// <summary>
        /// Move to the next result set
        /// </summary>
        /// <returns></returns>
        public override bool Next()
        {

            if(_start != null)
            {
                switch (typeof(T).Name.ToLower())
                {
                    case "datetime":

                        _start = ((DateTime)_start + (TimeSpan)Count);
                        break;

                    case "byte":
                    case "int16":
                    case "int32":
                    case "int64":

                        if (Entries.Count < (long)Convert.ChangeType(Count, typeof(long))) return false;

                        _start = ((long)_start + (long)Count);
                        break;

                    default:
                        return false;
                }
            }

            Page++;

            OnPropertyChanged("Start");
            OnPropertyChanged("End");

            OnReload(this);

            return true;

        }

        /// <summary>
        /// Move to the previous result set
        /// </summary>
        /// <returns></returns>
        public override bool Previous()
        {

            if(_start != null)
            {
                switch (typeof(T).Name.ToLower())
                {
                    case "datetime":

                        _start = ((DateTime)_start - (TimeSpan)Count);
                        break;

                    case "byte":
                    case "int16":
                    case "int32":
                    case "int64":

                        if ((long)_start == 0L) return false;

                        _start = Math.Max(0L, (long)_start - (long)Count);
                        break;

                    default:
                        return false;
                }
            }

            Page = Math.Max(0, --Page);

            OnPropertyChanged("Start");
            OnPropertyChanged("End");

            OnReload(this);

            return Page > 0;

        }

        public override void Reset()
        {

            while (_page > 0)
                Previous();

            base.Reset();
        }

    }
}
