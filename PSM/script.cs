using System;
using System.IO;

namespace PSMonitor.Powershell
{
    
    class Script : IDisposable
    {

        public FileInfo               File { get; private set; }

        public double                 Interval { get; set; } = Setup.Powershell.defaultInterval;

        public string                 Path { get; set; } = Setup.Powershell.defaultNamespace;

        public ScriptExecutionContext Context { get; private set; }
        
        public Script(Script script)
        {

            File = script.File;
            Interval = script.Interval;
            Context = new ScriptExecutionContext(this);

        }

        public Script(FileInfo fileInfo)
        {

            File = fileInfo;
            Interval = 1000;
            
            Context = new ScriptExecutionContext(this);

        }

        public void Dispose ()
        {
            Context.Dispose();
        }

    }
}
