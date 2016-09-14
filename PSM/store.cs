/// <copyright file="store.cs" company="Baker Hughes Incorporated">
/// Copyright (c) 2015 All Rights Reserved
/// </copyright>
/// <author>Odd Marthon Lende</author>
/// <summary>Store abstract class</summary>


using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Timers;
namespace PSMonitor.Stores
{

    public class Setup : PSMonitor.Setup
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
        public class SettingsCollection : ConfigurationElementCollection
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

        public class SettingElement : ConfigurationElement
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

    
    public abstract class Store : IStore
    {

        public abstract Enum Default { get; }

        public abstract Type Index { get; }

        public class Configuration : IOptions
        {

            private Type _store;

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
        /// Extends the <see cref="PSMonitor.Path"/> with some additional properties
        /// </summary>
        protected class Path : PSMonitor.Path
        {

            /// <summary>
            /// The index used when polling for new data.
            /// </summary>
            public object StartIndex { get; set; }

            /// <summary>
            /// The index used when fetching data
            /// </summary>
            public Enum Index { get; set; }

            /// <summary>
            /// The handler that will receive the data, when there is new data available.
            /// </summary>
            public RealTimeData Handler { get; set; }

            /// <summary>
            /// The constructor
            /// </summary>
            /// <param name="path">The <see cref="PSMonitor.Path"/> to extend.</param>
            public Path(PSMonitor.Path path)
            {
                this.Namespace = path.Namespace;
                this.Key = path.Key;
                this._components = path.Components;
            }

            /// <summary>
            /// <see cref="PSMonitor.Path.Extract(string)"/>
            /// </summary>
            public static new Path Extract(string path)
            {
                return new Path(PSMonitor.Path.Extract(path));
            }

        }
        
        /// <summary>
        /// Holds the registered receivers of realtime data updates.
        /// </summary>
        protected ConcurrentDictionary<object, ConcurrentBag<Path>> Receivers = new ConcurrentDictionary<object, ConcurrentBag<Path>>();

        /// <summary>
        /// <see cref="IStore.Delete(string)"/>
        /// </summary>
        public abstract void Delete(string path);

                
        /// <summary>
        /// <see cref="IStore.Read(string, object, object, Enum)"/>
        /// </summary>
        public abstract IEnumerable<Entry> Read(string path, object start, object end, Enum index);
        
        /// <summary>
        /// <see cref="IStore.Keys(string)"/>
        /// </summary>
        public abstract Key[] Keys(string ns);

        /// <summary>
        /// <see cref="IStore.Write(Envelope)"/>
        /// </summary>
        public abstract void Write(Envelope envelope);

        /// <summary>
        /// <see cref="IStore.Register(object, string, object, RealTimeData)"/>
        /// </summary>
        public virtual void Register(object context, string path, object startingIndex, Enum index, RealTimeData handler)
        {

            ConcurrentBag<Path> paths = null;

            foreach (object o in new object[] { context, path, startingIndex, handler })
            {
                if (o == null)
                    throw new NullReferenceException();
            }

            if (!Receivers.ContainsKey(context))
            {
                paths = new ConcurrentBag<Path>();
                while (!Receivers.TryAdd(context, paths)) ;
            }
            else
            {
                while (!Receivers.TryGetValue(context, out paths)) ;
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

            p1.StartIndex = startingIndex;
            p1.Handler = handler;
            p1.Index = index;

        }

        /// <summary>
        /// <see cref="IStore.Unregister(object)"/>
        /// </summary>
        public virtual void Unregister(object context)
        {
            ConcurrentBag<Path> list;

            if (!Receivers.ContainsKey(context))
                return;

            while (!Receivers.TryRemove(context, out list)) ;
        }

        /// <summary>
        /// <see cref="IStore.Unregister(object, string)"/>
        /// </summary>
        public virtual void Unregister(object context, string path)
        {

            ConcurrentBag<Path> list;

            if (!Receivers.ContainsKey(context))
                return;

            while (!Receivers.TryGetValue(context, out list)) ;

            Path p1 = Path.Extract(path);

            list.TakeWhile((p2) => { return p1 == p2; });


        }

        /// <summary>
        /// <see cref="IDisposable.Dispose"/>
        /// </summary>
        public virtual void Dispose()
        {

            foreach (KeyValuePair<object, IStore> pair in PSM._instances.ToArray())
            {

                IStore store;

                if (pair.Value == this)
                {
                    while(!PSM._instances.TryRemove(pair.Key, out store));
                }                    

            }

        }
                
    }

}
