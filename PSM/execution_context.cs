﻿using System;
using System.IO;
using System.Threading;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace PSMonitor
{

    delegate void DataAvailable(Envelope data);
    
    class ScriptExecutionContext : IDisposable
    {
        
        private const string ScriptExecutionContextID = "ScriptExecutionContextID";
        private string ID;
        private volatile Script script;
        private volatile IAsyncResult async;
        private volatile PowerShell   powerShell;
        private volatile ConcurrentQueue<Entry> queue;
        private List<Thread> threads;
        private DataAvailable OnData;

        private bool disposed = false;
        
        public ScriptExecutionContext(Script s)
        {
            
            ID = Guid.NewGuid().ToString();

            queue = new ConcurrentQueue<Entry>();

            script = s;
            script.executionContext = this;

            InitialSessionState state = InitialSessionState.CreateDefault();

            state.Commands.Add(new SessionStateCmdletEntry("set-interval", typeof(SetIntervalCommand), null));
            state.Commands.Add(new SessionStateCmdletEntry("set-namespace", typeof(SetNamespaceCommand), null));
            state.Commands.Add(new SessionStateCmdletEntry("set-timeout", typeof(SetTimeoutCommand), null));
            state.Commands.Add(new SessionStateCmdletEntry("push-data", typeof(PushDataCommand), null));
            state.Commands.Add(new SessionStateCmdletEntry("pop-data", typeof(PopDataCommand), null));
            state.Commands.Add(new SessionStateCmdletEntry("get-cda", typeof(GetCDACommand), null));
            state.Commands.Add(new SessionStateCmdletEntry("set-cda", typeof(SetCDACommand), null));

            state.Variables.Add(new SessionStateVariableEntry(ScriptExecutionContextID, ID, "The Guid of the ScriptExecutionContext that executed this script", ScopedItemOptions.Constant));

            SetIntervalCommand.Changed  += IntervalChanged;
            SetNamespaceCommand.Changed += NamespaceChanged;
            SetTimeoutCommand.Changed   += TimeoutChanged;
            PushDataCommand.Pushed      += DataPushed;
            PopDataCommand.Popped       += DataPopped;

            powerShell = PowerShell.Create(state).AddScript(File.ReadAllText(script.file.FullName));

            threads = new List<Thread>(3);

            threads.Add(new Thread(CheckErrors));
            threads.Add(new Thread(CheckTimeout));
            threads.Add(new Thread(WaitForResult));

            threads.ForEach(thread => thread.Start(this));

        }

        public bool IsCompleted
        {
            get
            {
                return (async == null) || (async != null && async.IsCompleted);
            }
        }

        private void IntervalChanged(SetIntervalCommand sender) {

            string id = (string)sender.GetVariableValue(ScriptExecutionContextID);

            if (id == ID)
                script.interval = sender.interval;
        }

        private void NamespaceChanged(SetNamespaceCommand sender) {

            string id = (string)sender.GetVariableValue(ScriptExecutionContextID);

            if (id == ID)
                script.path = sender.path;

        }

        private void TimeoutChanged(SetTimeoutCommand sender) {

            string id = (string)sender.GetVariableValue(ScriptExecutionContextID);

            if (id == ID)
                script.timeout = sender.timeout;

        }

        private void DataPushed(PushDataCommand sender)
        {

            string id = (string)sender.GetVariableValue(ScriptExecutionContextID);

            if (id == ID)
            {

                queue.Enqueue(new Entry()
                {

                    key       = sender.key,
                    value     = sender.data.BaseObject,
                    type      = sender.data.BaseObject.GetType(),
                    timestamp = DateTime.Now

                });

            }

        }

        private void Flush (bool all)
        {

            int count = queue.Count;
            Entry entry;            
            Envelope env = new Envelope()
            {
                path = script.path,
                entries = new Entry[all ? count : 1],
                timestamp = DateTime.Now
            };

            for(int i = 0; i < count; i++)
            {

                while(!queue.TryDequeue(out entry));

                env.entries[i] = entry;

                if (!all)
                    break;
            }
            
            OnData(env);

        }

        private void DataPopped(PopDataCommand sender)
        {

            string id = (string)sender.GetVariableValue(ScriptExecutionContextID);

            if (id == ID)
            {

                Flush(sender.flushAll == null || sender.flushAll == false);

            }

        }

        private static void WaitForResult(object ctx)
        {
            
            ScriptExecutionContext context = (ScriptExecutionContext) ctx;

            while (!context.disposed)
            {

                if (context.async != null)
                {

                    try {

                        foreach (PSObject obj in context.powerShell.EndInvoke(context.async))
                        {
                            Logger.info(obj.ToString());
                        }

                    }
                    catch(PipelineStoppedException)
                    {

                    }
                    catch (Exception e)
                    {
                        Logger.error(e.Message);
                    }

                    context.Flush(true);
                    context.async = null;

                }

                Thread.Sleep(1);

            }
            
        }

        private static void CheckTimeout(object ctx)
        {

            ScriptExecutionContext context = (ScriptExecutionContext)ctx;

            while (!context.disposed)
            {

                if (context.async != null)
                {

                    if (context.script.hasTimedOut)
                    {
                        Logger.error(String.Format("Aborted, Reason => Timeout exceeded for script '{0}'", context.script.file.Name));
                        context.Abort();
                    }

                }

                Thread.Sleep(1);
            }
        }

        private static void CheckErrors(object ctx)
        {
            ScriptExecutionContext context    = (ScriptExecutionContext)ctx;
            PowerShell             powerShell = context.powerShell;
            
            while (!context.disposed)
            {

                foreach (ErrorRecord errorRecord in powerShell.Streams.Error)
                {
                    Logger.error(errorRecord.ToString());                    
                }

                powerShell.Streams.Error.Clear();

                Thread.Sleep(1);

            }
        }

        public void Abort()
        {

            Entry entry;

            if (disposed) return;           

            powerShell.Stop();

            while (queue.Count > 0 && !queue.TryDequeue(out entry));
        }

        public void Execute(DataAvailable handler)
        {

            OnData = handler;

            if (async != null)
                return;

            powerShell.Streams.ClearStreams();

            script.lastExecutionTime = DateTime.Now;

            async = powerShell.BeginInvoke();

        }

        public void Dispose ()
        {

            Abort();

            SetIntervalCommand.Changed  -= IntervalChanged;
            SetNamespaceCommand.Changed -= NamespaceChanged;
            SetTimeoutCommand.Changed   -= TimeoutChanged;
            PushDataCommand.Pushed      -= DataPushed;
            PopDataCommand.Popped       -= DataPopped;

            disposed = true;

            threads.ForEach(thread => thread.Join());

            powerShell.Dispose();
            
        }

    }
}