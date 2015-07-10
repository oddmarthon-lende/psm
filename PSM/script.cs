using System;
using System.IO;

namespace PSMonitor
{
    
    class Script : IDisposable
    {
        public FileInfo               file;
        public double                 interval = Setup.Master.defaultInterval;
        public double                 timeout  = Setup.Master.defaultTimeout;
        public string                 path     = Setup.Master.defaultNamespace;
        public DateTime               lastExecutionTime;
        public ScriptExecutionContext executionContext;

        public bool shouldExecute
        {
            get
            {
               return isCompleted && DateTime.Now.Subtract(lastExecutionTime).TotalMilliseconds > interval;
            }
        }

        public bool hasTimedOut
        {
            get
            {
                return timeout > 0 && !isCompleted && DateTime.Now.Subtract(lastExecutionTime).TotalMilliseconds > timeout;
            }
        }

        public bool isCompleted
        {
            get
            {
                return executionContext.IsCompleted;
            }
        }

        public Script(Script s)
        {

            file = s.file;
            interval = s.interval;
            lastExecutionTime = s.lastExecutionTime;
            executionContext = new ScriptExecutionContext(this);

        }

        public Script(FileInfo f)
        {

            file = f;
            interval = 1000;
            lastExecutionTime = new DateTime(1970, 01, 01);
            executionContext = new ScriptExecutionContext(this);

        }

        public void Dispose ()
        {
            executionContext.Dispose();
        }

    }
}
