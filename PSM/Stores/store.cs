using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace PSMonitor.Stores
{

    public class Setup : PSMonitor.Setup
    {

        public static TResult Get<T, TResult>(string name)
        {

            SettingsCollection settings = PSMonitor.Setup.Get<Setup>("stores").Settings;

            foreach (SettingElement element in settings)
            {
                if ((System.Type.GetType(element.For, false, true) ?? typeof(object)).Equals(typeof(T)) && element.Name == name && !String.IsNullOrEmpty(element.Value))
                    return (TResult)Convert.ChangeType(element.Value, typeof(TResult));
            }

            throw new ConfigurationErrorsException(String.Format("Can not find configuration key with the name: {0}", name));
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
                return ((SettingElement)element).ElementInformation.LineNumber - base.ElementInformation.LineNumber - 1;
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
            public string Value { get { return (string)base["value"]; } }

        }

        [ConfigurationProperty("type", DefaultValue = "", IsRequired = false)]
        public string Type { get { return (string)base["type"]; } }

        [ConfigurationProperty("settings", IsRequired = false)]
        public SettingsCollection Settings { get { return (SettingsCollection)base["settings"]; } }

    }

    public abstract class Store : IStore
    {       

        /// <summary>
        /// <see cref="IStore.Options"/>
        /// </summary>
        public virtual object Options { get; protected set; }

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
        public abstract long Delete(string path);

        /// <summary>
        /// <see cref="IStore.Delete(string, DateTime, DateTime)"/>
        /// </summary>
        public abstract long Delete(string path, DateTime start, DateTime end);

        /// <summary>
        /// <see cref="IStore.Get(string)"/>
        /// </summary>
        public abstract Entry Get(string path);

        /// <summary>
        /// <see cref="IStore.Get(string, long, long)"/>
        /// </summary>
        public abstract IEnumerable<Entry> Get(string path, long start, long end);

        /// <summary>
        /// <see cref="IStore.Get(string, DateTime, DateTime)"/>
        /// </summary>
        public abstract IEnumerable<Entry> Get(string path, DateTime start, DateTime end);

        /// <summary>
        /// <see cref="IStore.GetKeys(string)"/>
        /// </summary>
        public abstract Key[] GetKeys(string ns);

        /// <summary>
        /// <see cref="IStore.Put(Envelope)"/>
        /// </summary>
        public abstract void Put(Envelope envelope);

        /// <summary>
        /// <see cref="IStore.Register(object, string, object, RealTimeData)"/>
        /// </summary>
        public virtual void Register(object context, string path, object startingIndex, RealTimeData handler)
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

            foreach (KeyValuePair<object, IStore> pair in PSM.Pool.ToArray())
            {

                IStore store;

                if (pair.Value == this)
                {
                    while(!PSM.Pool.TryRemove(pair.Key, out store));
                }                    

            }

        }

    }

}
