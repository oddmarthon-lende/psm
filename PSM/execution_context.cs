/// <copyright file="execution_context.cs" company="Baker Hughes Incorporated">
/// Copyright (c) 2015 All Rights Reserved
/// </copyright>
/// <author>Odd Marthon Lende</author>
/// 
using System;
using System.IO;
using System.Threading;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace PSM.Powershell
{
    
    
    class ScriptExecutionContext : IDisposable
    {
        
        private const string _scriptExecutionContextIDString = "ScriptExecutionContextID";

        private string _id;

        private volatile Script _script;

        private volatile PowerShell   _powerShell;

        private volatile ConcurrentQueue<Entry> _queue;

        private Thread _thread;

        private RealTimeData _onData;

        private bool _disposed = false;

        private DateTime _lastExecutionTime = new DateTime(1970, 01, 01);
        
        public ScriptExecutionContext(Script s)
        {
            
            _id = Guid.NewGuid().ToString();
            _queue = new ConcurrentQueue<Entry>();
            _script = s;

            InitialSessionState state = InitialSessionState.CreateDefault();

            state.Commands.Add(new SessionStateCmdletEntry("set-interval", typeof(SetIntervalCommand), null));
            state.Commands.Add(new SessionStateCmdletEntry("set-namespace", typeof(SetNamespaceCommand), null));
            state.Commands.Add(new SessionStateCmdletEntry("push-data", typeof(PushDataCommand), null));

            state.Variables.Add(new SessionStateVariableEntry(_scriptExecutionContextIDString, _id, "The Guid of the ScriptExecutionContext that executed this script", ScopedItemOptions.Constant));

            SetIntervalCommand.Changed  += IntervalChanged;
            SetNamespaceCommand.Changed += NamespaceChanged;
            PushDataCommand.Pushed      += DataPushed;

            _powerShell = PowerShell.Create(state).AddScript(File.ReadAllText(_script.File.FullName));

            _thread = new Thread(Execute);
            _thread.Name = _script.File.ToString();
        }
        
        private void IntervalChanged(SetIntervalCommand sender) {

            string id = (string)sender.GetVariableValue(_scriptExecutionContextIDString);

            if (id == _id)
            {
                _script.Interval = sender.Interval;

                Debug.WriteLine(String.Format("{0}: Interval set to {1}", _script.File.FullName, sender.Interval));
            }
        }

        private void NamespaceChanged(SetNamespaceCommand sender) {

            string id = (string)sender.GetVariableValue(_scriptExecutionContextIDString);

            if (id == _id) {

                Flush(true);

                _script.Path = sender.Path;

                Debug.WriteLine(String.Format("{0}: Namespace set to {1}", _script.File.FullName, sender.Path));
                
            }               

        }

        private void DataPushed(PushDataCommand sender)
        {

            string id = (string)sender.GetVariableValue(_scriptExecutionContextIDString);

            if (id == _id)
            {
                Entry entry;

                _queue.Enqueue(entry = new Entry()
                {

                    Key = sender.Key,
                    Value = sender.Data.BaseObject,
                    Type = sender.Data.BaseObject.GetType(),
                    Timestamp = sender.Timestamp ?? DateTime.Now

                });

                Debug.WriteLine(String.Format("{0}: Added data to queue (Key: {1}, Value: {2})", _script.File.FullName, entry.Key, entry.Value));

            }

        }

        private void Flush (bool all)
        {

            int count = _queue.Count;
            Entry entry;
                        
            Envelope env = new Envelope()
            {
                Path = _script.Path,
                Entries = new Entry[all ? count : 1],
                Timestamp = DateTime.Now
            };

            if(count > 0)
                Logger.Info(String.Format("*** {0} ***", _script.Path));

            for (int i = 0; i < count; i++)
            {

                while(!_queue.TryDequeue(out entry));

                env.Entries[i] = entry;
                
                Logger.Info(String.Format(" {0} : {1}", entry.Key, entry.Value));

                if (!all)
                    break;
            }
            
            if(env.Entries.Length > 0)
            {
                _onData(env);
                Debug.WriteLine("Flushed " + env.Entries.Length + " entries");

            }
                

        }

        private static void Execute(object ctx)
        {

            ScriptExecutionContext context = (ScriptExecutionContext)ctx;
            PowerShell powerShell = context._powerShell;

            while (!context._disposed)
            {
                try {
                    Thread.Sleep(100);
                }
                catch(ThreadInterruptedException) { };


                if (DateTime.Now.Subtract(context._lastExecutionTime).TotalMilliseconds > context._script.Interval)
                {
                    
                    context._powerShell.Streams.ClearStreams();
                    context._lastExecutionTime = DateTime.Now;

                    try
                    {
                        Logger.Info(String.Format("Executing script: {0}", context._script.File.Name));
                        context._powerShell.Invoke();
                    }
                    catch(ThreadInterruptedException) { }
                    
                }

                foreach (ErrorRecord errorRecord in powerShell.Streams.Error)
                {
                    Logger.Error(String.Format("Powershell : {0} : {1}", context._script.File.Name, errorRecord.ToString()));
                }

                powerShell.Streams.Error.Clear();

                context.Flush(true);
            }
        }
        

        public void Abort()
        {

            Entry entry;

            if (_disposed) return;           

            _powerShell.Stop();

            while (_queue.Count > 0 && !_queue.TryDequeue(out entry));
        }

        public void Start(RealTimeData handler)
        {

            _onData = handler;
            _thread.Start(this);

        }

        public void Dispose ()
        {

            Abort();

            SetIntervalCommand.Changed  -= IntervalChanged;
            SetNamespaceCommand.Changed -= NamespaceChanged;
            PushDataCommand.Pushed      -= DataPushed;

            _disposed = true;

            _thread.Interrupt();
            _thread.Join();

            _powerShell.Dispose();
            
        }

    }
}
