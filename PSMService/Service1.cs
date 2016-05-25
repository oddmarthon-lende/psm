using System.ServiceProcess;
using System.IO;
using System.Diagnostics;
using System;

namespace PSMonitor
{
    public partial class Service1 : ServiceBase
    {
        private static Powershell.Powershell ps = new Powershell.Powershell();
        
        private Log LogToStore = (message, type) =>
        {

            if (type != EventLogEntryType.Error && type != EventLogEntryType.FailureAudit)
                return;

            PSM.Store().Put(new Envelope()
            {

                Path = "@errors",
                Entries = new Entry[1] {

                    new Entry {

                        Key = Environment.MachineName,
                        Type = typeof(string),
                        Value = message,
                        Timestamp = DateTime.Now

                    }
                }
            });

        };

        private FileSystemWatcher watcher;

        public Service1()
        {

            Logger.OnLog += LogToStore;

            InitializeComponent();

            watcher = new FileSystemWatcher(Setup.Powershell.Directory);

            watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.CreationTime;
            watcher.Filter       = "*.ps1";

            watcher.Changed += new FileSystemEventHandler(OnChanged);
            watcher.Created += new FileSystemEventHandler(OnChanged);
            watcher.Deleted += new FileSystemEventHandler(OnChanged);
            watcher.Renamed += new RenamedEventHandler(OnRenamed);

        }

        public new void Dispose()
        {
            Logger.OnLog -= LogToStore;
            base.Dispose();
        }

        protected override void OnStart(string[] args)
        {

            if (Directory.Exists(Setup.Powershell.Directory))
            {
                ps.Reload();
                watcher.EnableRaisingEvents = true;
            }
            else
            {
                Logger.Failure("Directory does not exist");
            }
        }

        protected override void OnStop()
        {

            watcher.EnableRaisingEvents = false;
            watcher.Dispose();
            ps.Dispose();

        }

        private void OnChanged(object src, FileSystemEventArgs e)
        {
            ps.Reload();
        }

        private void OnRenamed(object src, FileSystemEventArgs e)
        {
            ps.Reload();
        }
    }
}
