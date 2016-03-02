/// <copyright file="master.cs" company="Baker Hughes Incorporated">
/// Copyright (c) 2015 All Rights Reserved
/// </copyright>
/// <author>Odd Marthon Lende</author>
/// 
using System.Collections.Generic;
using System;
using System.IO;
using System.Timers;
using System.Collections;
using System.Collections.Concurrent;
using PSMonitor.Stores;
using System.Configuration;
using System.Reflection;

namespace PSMonitor
{

    public class Setup : ConfigurationSection
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
                using (Stream original = _assembly.GetManifestResourceStream("PSMonitor.psm.config"))
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

        [ConfigurationProperty("defaultTimeout", DefaultValue = "5000", IsRequired = false)]
        public int defaultTimeout { get { return (int)base["defaultTimeout"]; } }

        [ConfigurationProperty("defaultInterval", DefaultValue = "60000", IsRequired = false)]
        public int defaultInterval { get { return (int)base["defaultInterval"]; } }

        [ConfigurationProperty("defaultNamespace", DefaultValue = "root", IsRequired = false)]
        public string defaultNamespace { get { return (string)base["defaultNamespace"]; } }

        [ConfigurationProperty("timerFrequency", DefaultValue = 1000, IsRequired = false)]
        public int timerFrequency { get { return (int)base["timerFrequency"]; } }

        public static Setup Master {  get { return Get<Setup>("master"); } }

        public static T Get<T> (string section)
        {
            
            if (!config.HasFile)
                throw new FileNotFoundException(String.Format("Missing config file: {0}", _map.ExeConfigFilename));

            return (T)(object)config.GetSection(section);

        }        

    }

    public sealed class PSM : IDisposable, IEnumerable<Envelope>
    {   
        
        public class Entries : BlockingCollection<Envelope>, IEnumerator<Envelope>, IDisposable
        {
            
            public Envelope Current
            {
                get
                {
                    return base.Take();
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    return Current;
                }
            }

            public new void Dispose()
            {
                base.Dispose();
            }

            public bool MoveNext()
            {
                return true;
            }

            public void Reset() { }
        }

        private Entries entries { get; set; }

        private System.Timers.Timer timer { get; set; }

        private List<Script> scripts { get; set; }

        internal static ConcurrentDictionary<object, IStore> Pool = new ConcurrentDictionary<object, IStore>();
                
        public static IStore Store(object context = null)
        {

            Type store = Type.GetType(Setup.Get<Stores.Setup>("stores").Type, false, true);

            lock (Pool)
            {

                if (context == null)
                    context = typeof(IStore);

                IStore instance = null;
                
                if ( !Pool.ContainsKey(context) )
                {                    

                    if (store != null)
                    {

                        store.FindInterfaces((m, f) =>
                        {
                            if (m.Equals(f))
                            {
                                instance = (IStore)Activator.CreateInstance(store);
                                return true;
                            }

                            return false;

                        }, typeof(IStore));


                        while (!Pool.TryAdd(context, instance)) ;


                    }
                    else
                    {
                        Logger.Warn("No store was configured, data will be accumulated in memory");
                    }

                }

                if (Pool.ContainsKey(context))
                    while (!Pool.TryGetValue(context, out instance)) ;

                return instance;

            }

        }
        
        public PSM()
        {           

            entries        = new Entries();
            scripts        = new List<Script>();
            timer          = new System.Timers.Timer(Setup.Master.timerFrequency);
            timer.Elapsed += new ElapsedEventHandler(OnTick);
            
            timer.Start();

        }
        
        public void Dispose()
        {

            timer.Stop();
            timer.Dispose();

            scripts.ForEach(script => script.Dispose());
            scripts.Clear();

            entries.Dispose();                      

        }

        internal IEnumerator<Envelope> GetEnumerator()
        {
            return entries;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return entries;
        }

        IEnumerator<Envelope> IEnumerable<Envelope>.GetEnumerator()
        {
            return entries;
        }

        public void Reload()
        {
            
            DirectoryInfo         dir   = new DirectoryInfo(Setup.Master.Directory);
            IEnumerable<FileInfo> files = dir.EnumerateFiles("*.ps1");
            
            foreach( FileInfo file in files)
            {

                int i = scripts.FindIndex(s => { return s.file.FullName == file.FullName; });

                if (i == -1)

                    scripts.Add(new Script(file));

                else
                {
                    scripts[i].Dispose();
                    scripts[i] = new Script(scripts[i]);
                }
            }

            {

                int result = scripts.RemoveAll(s => !File.Exists(s.file.FullName));

                if (result > 0)

                    Logger.Info("Removed" + result + " files");

            }
            
        }

        private object OnData(Envelope data)
        {

            IStore store = Store(this.GetType());

            if (store != null)
            {

                try
                {
                    store.Put(data);
                }
                catch (Exception error)
                {
                    Logger.Error(error.Message);
                }

            }
            else 
                entries.Add(data);

            return null;
        }

        private void OnTick(object src, ElapsedEventArgs e)
        {
            
            foreach(Script script in scripts)
            {

                if (script.executionContext.IsCompleted)
                {

                    if (script.shouldExecute)
                        script.executionContext.Execute(OnData);

                }

            }

        }

    }
}
