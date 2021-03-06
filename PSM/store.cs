﻿/// <copyright file="store.cs" company="Baker Hughes Incorporated">
/// Copyright (c) 2015 All Rights Reserved
/// </copyright>
/// <author>Odd Marthon Lende</author>
/// <summary>Store abstract class</summary>


using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
/// <copyright file="store.cs" company="Baker Hughes Incorporated">
/// Copyright (c) 2016 All Rights Reserved
/// </copyright>
/// <author>Odd Marthon Lende</author>
/// 
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace PSM.Stores
{
    /// <summary>
    /// 
    /// </summary>
    internal class Setup : global::PSM.Setup
    {

        public static TResult Get<T, TResult>(string name, bool allowEmpty = false)
        {

            SettingsCollection settings = Get<Setup>("stores").Settings;

            foreach (SettingElement element in settings)
            {
                if ((System.Type.GetType(element.For, false, true) ?? typeof(object)).Equals(typeof(T)) && element.Name == name && (allowEmpty || !String.IsNullOrEmpty(element.Value)) )
                    return (TResult)Convert.ChangeType(element.Value, typeof(TResult));
            }

            throw new ConfigurationErrorsException(String.Format("Could not find configuration key with the name: {0}", name));
        }

        [ConfigurationCollection(typeof(SettingElement), CollectionType = ConfigurationElementCollectionType.AddRemoveClearMap)]
        internal class SettingsCollection : ConfigurationElementCollection
        {
            protected override ConfigurationElement CreateNewElement()
            {
                return new SettingElement();
            }

            protected override object GetElementKey(ConfigurationElement element)
            {
                SettingElement s = (SettingElement)element;
                return String.Format("{0}.{1}", s.For, s.Name);
            }

            [ConfigurationProperty("setting", IsRequired = false)]
            public SettingElement Setting { get { return (SettingElement)base["setting"]; } }

        }

        internal class SettingElement : ConfigurationElement
        {

            [ConfigurationProperty("for", IsRequired = true)]
            public string For { get { return (string)base["for"]; } }

            [ConfigurationProperty("name", IsRequired = true)]
            public string Name { get { return (string)base["name"]; } }

            [ConfigurationProperty("value", IsRequired = true)]
            public string Value {
                get { return (string)base["value"]; }
                set { base["value"] = value; }
            }

        }

        [ConfigurationProperty("type", DefaultValue = "", IsRequired = false)]
        public string Type {
            get { return (string)base["type"]; }
            set { base["type"] = value; }
        }

        [ConfigurationProperty("settings", IsRequired = false)]
        public SettingsCollection Settings { get { return (SettingsCollection)base["settings"]; } }
        
    }

    /// <summary>
    /// Abstract class for implementing a store
    /// </summary>
    public abstract class Store : IStore
    {
        /// <summary>
        /// 
        /// </summary>
        public abstract Enum Default { get; }

        /// <summary>
        /// 
        /// </summary>
        public abstract Type Index { get; }

        /// <summary>
        /// 
        /// </summary>
        public class Configuration : IOptions
        {
            /// <summary>
            /// 
            /// </summary>
            private Type _store;

            /// <summary>
            /// 
            /// </summary>
            [Category("Data")]
            [Description("The data store used when loading data")]
            public Type Store {

                get
                {
                    return _store == null ? Type.GetType(Setup.Get<Stores.Setup>("stores").Type, false, true) : _store;
                }

                set
                {
                    _store = value;
                                        
                }
            }
            
            /// <summary>
            /// 
            /// </summary>
            /// <returns></returns>
            public virtual Properties Get()
            {

                Properties properties = new Properties();
                
                foreach (PropertyDescriptor descriptor in TypeDescriptor.GetProperties(this) )
                {

                    properties.Add(descriptor, new List<KeyValuePair<object, object>>());

                    switch(descriptor.Name)
                    {
                        
                        case "Store":
                            
                            foreach(Type type in Assembly.GetExecutingAssembly().GetTypes())
                            {

                                if (type.IsAbstract)
                                    continue;

                                type.FindInterfaces((m, f) =>
                                {
                                    
                                    if (m.Equals(f))
                                    {
                                        properties.Last().Value.Add(new KeyValuePair<object, object>(type.Name, type));
                                        return true;
                                    }

                                    return false;

                                }, typeof(IStore));
                                
                            }

                            break;
                    }
                }

                return properties;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="name"></param>
            /// <returns></returns>
            public T Get<T>(string name)
            {
                object value = this.GetType().GetProperty(name).GetValue(this);

                try
                {
                    return (T)value;
                }
                catch(Exception)
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
            }

        }

        /// <summary>
        /// <see cref="IStore.Options"/>
        /// </summary>
        public virtual IOptions Options { get; protected set; } = new Configuration();

        /// <summary>
        /// Extends the <see cref="global::PSM.Path"/> with some additional properties
        /// </summary>
        protected class Path : global::PSM.Path
        {

            /// <summary>
            /// The index used when polling for new data.
            /// </summary>
            public Index StartIndex { get; set; } = new Index();

            /// <summary>
            /// The index used when fetching data
            /// </summary>
            public Enum IndexIdentifier { get; set; }

            /// <summary>
            /// The handler that will receive the data, when there is new data available.
            /// </summary>
            public RealTimeData Handler { get; set; }

            /// <summary>
            /// The constructor
            /// </summary>
            /// <param name="path">The <see cref="global::PSM.Path"/> to extend.</param>
            public Path(global::PSM.Path path)
            {
                this.Namespace = path.Namespace;
                this.Key = path.Key;
                this._components = path.Components;
            }

            /// <summary>
            /// <see cref="global::PSM.Path.Extract(string)"/>
            /// </summary>
            public static new Path Extract(string path)
            {
                return new Path(global::PSM.Path.Extract(path));
            }

        }
        
        /// <summary>
        /// Holds the registered receivers of realtime data updates.
        /// </summary>
        protected ConcurrentDictionary<object, ConcurrentBag<Path>> _receivers = new ConcurrentDictionary<object, ConcurrentBag<Path>>();

        /// <summary>
        /// If <c>true</c>, this object has been disposed.
        /// </summary>
        protected bool _disposed = false;

        /// <summary>
        /// Frequency counter for packets\envelopes received
        /// </summary>
        protected Frequency _freqIn = new Frequency(Frequency.RateUnit.MINUTES, Frequency.RateUnit.SECONDS);

        /// <summary>
        /// Frequency counter for packets\envelopes sent
        /// </summary>
        protected Frequency _freqOut = new Frequency(Frequency.RateUnit.MINUTES, Frequency.RateUnit.SECONDS);

        /// <summary>
        /// Holds the envelopes that are waiting to be dispatched to the database.
        /// </summary>
        protected ConcurrentQueue<Envelope> _queue;

        /// <summary>
        /// The threads created by this instance
        /// </summary>
        protected IReadOnlyCollection<Thread> _threads;

        /// <summary>
        /// The ID of this instance
        /// </summary>
        protected Guid _id = Guid.NewGuid();

        /// <summary>
        /// <see cref="IStore.Output"/>
        /// </summary>
        public event RealTimeData Output;

        /// <summary>
        /// Constructor
        /// </summary>
        public Store()
        {
            _queue = new ConcurrentQueue<Envelope>();
        }

        /// <summary>
        /// Triggers the <see cref="Output"/> event
        /// </summary>
        /// <param name="data"></param>
        protected virtual void OnOutput(Envelope data)
        {
            RealTimeData handler = Output;
            if (handler != null) handler(data);
        }

        /// <summary>
        /// <see cref="IStore.Delete(string)"/>
        /// </summary>
        public abstract void Delete(string path);

                
        /// <summary>
        /// <see cref="IStore.Read(string, object, object, Enum)"/>
        /// </summary>
        public abstract IEnumerable<Entry> Read(string path, IComparable start, IComparable end, Enum index);
        
        /// <summary>
        /// <see cref="IStore.Keys(string)"/>
        /// </summary>
        public abstract IEnumerable<Key> Keys(string ns);

        /// <summary>
        /// <see cref="IStore.Write(Envelope)"/>
        /// </summary>
        public virtual void Write(string path, params Entry[] entries)
        {

            if (!_disposed)
            {

                Envelope envelope = new Envelope()
                {
                    Entries = entries,
                    Path = path
                };

                _queue.Enqueue(envelope);
                _freqIn.Mark(1);
            }
        }

        /// <summary>
        /// <see cref="IStore.Register(object, string, object, RealTimeData)"/>
        /// </summary>
        public virtual void Register(object context, string path, IComparable startingIndex, Enum index, RealTimeData handler)
        {

            ConcurrentBag<Path> paths = null;

            foreach (object o in new object[] { context, path, startingIndex, handler })
            {
                if (o == null)
                    throw new NullReferenceException();
            }

            if (!_receivers.ContainsKey(context))
            {
                paths = new ConcurrentBag<Path>();
                while (!_receivers.TryAdd(context, paths)) ;
            }
            else
            {
                while (!_receivers.TryGetValue(context, out paths)) ;
            }

            Path p1 = Path.Extract(path);

            if (!paths.Contains(p1))
            {
                paths.Add(p1);
            }
            else
            {

                p1 = paths.Single((p2) =>
                {
                    return p2 == p1;
                });
            }

            p1.StartIndex = new Index(new KeyValuePair<Enum, IComparable>(index, startingIndex));
            p1.Handler = handler;
            p1.IndexIdentifier = index;

        }

        /// <summary>
        /// <see cref="IStore.Unregister(object)"/>
        /// </summary>
        public virtual void Unregister(object context)
        {
            ConcurrentBag<Path> list;

            if (!_receivers.ContainsKey(context))
                return;

            while (!_receivers.TryRemove(context, out list)) ;
        }

        /// <summary>
        /// <see cref="IStore.Unregister(object, string)"/>
        /// </summary>
        public virtual void Unregister(object context, string path)
        {

            ConcurrentBag<Path> list;

            if (!_receivers.ContainsKey(context))
                return;

            while (!_receivers.TryGetValue(context, out list)) ;

            Path p1 = Path.Extract(path);

            list.TakeWhile((p2) => { return p1 == p2; });


        }

        /// <summary>
        /// <see cref="IDisposable.Dispose"/>
        /// </summary>
        public virtual void Dispose()
        {

            foreach (KeyValuePair<object, IStore> pair in PSM.Store._instances.ToArray())
            {

                IStore store;

                if (pair.Value == this)
                {
                    while(!PSM.Store._instances.TryRemove(pair.Key, out store));
                }                    

            }

            _disposed = true;

        }

        /// <summary>
        /// Balances the queue and drops packets if the frequency is too high
        /// </summary>
        /// <param name="ctx">The <see cref="DB"/> instance</param>
        protected static void Balance(object ctx)
        {

            Store context = (Store)ctx;

            while (!context._disposed)
            {
                
                try
                {

                    Thread.Sleep((int)Frequency.RateUnit.MINUTES);

                    double frequency_in = context._freqIn.Rate(Frequency.RateUnit.MINUTES);
                    double frequency_out = context._freqOut.Rate(Frequency.RateUnit.MINUTES);

                    Debug.WriteLine(String.Format("Store.Balance() : Frequency In - {0}, Out - {1} per minute : Queue Size - {2} ", frequency_in, frequency_out, context._queue.Count));

                    double factor = frequency_in / frequency_out;

                    if (factor > 1.0)
                    {
                        Envelope env;

                        int count = double.IsInfinity(factor) ? context._queue.Count : context._queue.Count - (int)(context._queue.Count / factor);

                        for (int i = 0; i < count; i++)
                        {
                            context._queue.TryDequeue(out env);
                        }

                        Debug.WriteLine(String.Format("Store.Balance() : Dropped - {0} ", count));

                    }


                }
                catch (ThreadInterruptedException e)
                {
                    Debug.WriteLine(e.Message);
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                    Logger.Error(e);
                }



            }

        }

    }

}
