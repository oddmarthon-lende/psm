using System.Collections.Generic;
using System;
using System.IO;
using System.Timers;
using System.Collections;
using System.Collections.Concurrent;
using PSMonitor.Stores;
using System.Configuration;
using System.Threading;
using System.Linq;

namespace PSMonitor
{

    public class Setup : ConfigurationSection
    {
        
        private static ExeConfigurationFileMap map = new ExeConfigurationFileMap() { ExeConfigFilename = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "psm.config") };
        protected static Configuration config = ConfigurationManager.OpenMappedExeConfiguration(map, ConfigurationUserLevel.None);

        public Setup ()
        {
            
            if (!config.HasFile)
                throw new FileNotFoundException(String.Format("Missing config file: {0}", map.ExeConfigFilename));
        }

        [ConfigurationProperty("scriptDirectory", DefaultValue = "C:\\", IsRequired = false)]
        public string Directory { get { return (string)base["scriptDirectory"]; } }

        [ConfigurationProperty("defaultTimeout", DefaultValue = "1000", IsRequired = false)]
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
                throw new FileNotFoundException(String.Format("Missing config file: {0}", map.ExeConfigFilename));

            return (T)(object)config.GetSection(section);

        }

    }

    public class PSM : IDisposable, IEnumerable<Envelope>
    {   
        
        protected internal class Entries : BlockingCollection<Envelope>, IEnumerator<Envelope>, IDisposable
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

        protected Entries entries { get; set; }
        protected System.Timers.Timer timer { get; set; }
        private List<Script> scripts { get; set; }

        private static ConcurrentDictionary<Thread, IStore> Pool = new ConcurrentDictionary<Thread, IStore>();

        public static IStore Store {

            get {

                Thread thread   = Thread.CurrentThread;
                IStore instance = null;
                
                if (!Pool.ContainsKey(thread)) {

                    Type store = Type.GetType(Setup.Get<Stores.Setup>("stores").Type, false, true);

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

                        while(!Pool.TryAdd(thread, instance));
                                                

                    }
                    else
                    {
                        Logger.warn("No store was configured, data will be accumulated in memory");
                    }

                }

                if (Pool.ContainsKey(thread))
                    while (!Pool.TryGetValue(thread, out instance));

                foreach(Thread t in (from pair in Pool select pair.Key))
                {

                    IStore store = null;

                    if (!t.IsAlive && Pool.TryRemove(t, out store))
                    {
                        store.Dispose();
                    }

                }

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

            if (Store != null)
                Store.Dispose();

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

                    Logger.info("Removed" + result + " files");

            }
            
        }

        protected object OnData(Envelope data)
        {

            if(Store != null)
            {

                try
                {
                    Store.Put(data);
                }
                catch (Exception error)
                {
                    Logger.error(error.Message);
                }

            }
            else 
                entries.Add(data);

            return null;
        }

        protected void OnTick(object src, ElapsedEventArgs e)
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
