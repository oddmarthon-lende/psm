/// <copyright file="master.cs" company="Baker Hughes Incorporated">
/// Copyright (c) 2015 All Rights Reserved
/// </copyright>
/// <author>Odd Marthon Lende</author>
/// 
using System;
using System.IO;
using System.Collections.Concurrent;
using PSM.Stores;
using System.Configuration;
using System.Reflection;

namespace PSM
{
    /// <summary>
    /// The realtime data handler delegate that is invoked when new data arrives.
    /// </summary>
    /// <param name="data">The data that is being transferred.</param>
    /// <returns>A starting index from which the next transfer occurs.</returns>
    public delegate object RealTimeData(Envelope data);

    internal class Setup : ConfigurationSection
    {
        private static Assembly _assembly = Assembly.GetExecutingAssembly();

        private static ExeConfigurationFileMap _map = Map();

        protected static Configuration config = ConfigurationManager.OpenMappedExeConfiguration(_map, ConfigurationUserLevel.None);

        public Setup ()
        {          

            if (!config.HasFile)
                throw new FileNotFoundException(String.Format("Missing config file: {0}", _map.ExeConfigFilename));
        }

        private static ExeConfigurationFileMap Map()
        {

            string path = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(_assembly.Location), "psm.config");

            if (!File.Exists(path))
            {
                using (Stream original = _assembly.GetManifestResourceStream("PSM.psm.config"))
                {
                    using (FileStream file = new FileStream(path, FileMode.CreateNew))
                    {
                        original.CopyTo(file);
                    }
                }
            }

            return new ExeConfigurationFileMap() { ExeConfigFilename = path };

        }

        [ConfigurationProperty("scriptDirectory", DefaultValue = "C:\\", IsRequired = false)]
        public string Directory { get { return (string)base["scriptDirectory"]; } }

        [ConfigurationProperty("defaultInterval", DefaultValue = "60000", IsRequired = false)]
        public int defaultInterval { get { return (int)base["defaultInterval"]; } }

        [ConfigurationProperty("defaultNamespace", DefaultValue = "root", IsRequired = false)]
        public string defaultNamespace { get { return (string)base["defaultNamespace"]; } }
        

        public static Setup Powershell {  get { return Get<Setup>("powershell"); } }

        public static T Get<T> (string section)
        {
            
            if (!config.HasFile)
                throw new FileNotFoundException(String.Format("Missing config file: {0}", _map.ExeConfigFilename));

            return (T)(object)config.GetSection(section);

        }        

    }

    public sealed class Store
    {

        internal static ConcurrentDictionary<object, IStore> _instances = new ConcurrentDictionary<object, IStore>();
                
        /// <summary>
        /// Gets the store object for the context
        /// </summary>
        /// <param name="context">The context, used to have multiple stores running in different threads.</param>
        /// <param name="store">The store type, when using this parameter once, this will change the store for the context.</param>
        /// <returns>The store instance</returns>
        public static IStore Get(object context = null, Type store = null)
        {

            IStore instance = null;

            lock (_instances)
            {

                if (context == null)
                    context = typeof(IStore);

                if(store != null && _instances.ContainsKey(context))
                {

                    while (!_instances.TryRemove(context, out instance));
                    instance.Dispose();
                    instance = null;
                    
                }

                if (_instances.ContainsKey(context))
                    while (!_instances.TryGetValue(context, out instance));

                store = store ?? (instance != null ? instance.GetType() : Type.GetType(Setup.Get<Stores.Setup>("stores").Type, false, true));
                
                if ( !_instances.ContainsKey(context) )
                {

                    if (store != null)
                    {

                        if(store.FindInterfaces((m, f) =>
                        {
                            if (m.Equals(f))
                            {
                                instance = (IStore)Activator.CreateInstance(store);
                                return true;
                            }

                            return false;

                        }, typeof(IStore)).Length == 0)
                            throw new Exception("Object does not implement IStore interface");


                        while (!_instances.TryAdd(context, instance)) ;


                    }

                }

                if (instance == null && _instances.ContainsKey(context))
                    while (!_instances.TryGetValue(context, out instance)) ;

                return instance;

            }

        }
        
        
    }
}
