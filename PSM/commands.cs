using System;
using System.Management.Automation;

namespace PSMonitor
{
    
    public delegate void IntervalChanged(SetIntervalCommand sender);
    public delegate void NamespaceChanged(SetNamespaceCommand sender);
    public delegate void TimeoutChanged(SetTimeoutCommand sender);
    public delegate void DataPushed(PushDataCommand sender);
    public delegate void DataPopped(PopDataCommand sender);

    [Cmdlet(VerbsCommon.Set, "Interval")]
    public class SetIntervalCommand : PSCmdlet
    {

        public static event IntervalChanged Changed;
        
        [Parameter(
            Position = 0,
            Mandatory = true,
            HelpMessage = "Sets the interval the script will run in milliseconds")]
        [ValidateNotNullOrEmpty]
        public double interval
        {
            get; set;
        }

        protected override void ProcessRecord()
        {

            Changed(this);
            base.ProcessRecord();
        }
    }

    [Cmdlet(VerbsCommon.Set, "Namespace")]
    public class SetNamespaceCommand : PSCmdlet
    {

        public static event NamespaceChanged Changed;

        [Parameter(
            Position = 0,
            Mandatory = true,
            HelpMessage = "Sets the current namespace for the data output")]
        [ValidateNotNullOrEmpty]
        public string path
        {
            get; set;
        }

        protected override void ProcessRecord()
        {
            Changed(this);
            base.ProcessRecord();
        }
    }

    [Cmdlet(VerbsCommon.Set, "Timeout")]
    public class SetTimeoutCommand : PSCmdlet
    {

        public static event TimeoutChanged Changed;

        [Parameter(
            Position = 0,
            Mandatory = true,
            HelpMessage = "Sets the timeout, thread will exit if it exceeds timeout")]
        [ValidateNotNullOrEmpty]
        public double timeout
        {
            get; set;
        }

        protected override void ProcessRecord()
        {
            Changed(this);
            base.ProcessRecord();
        }
    }

    [Cmdlet(VerbsCommon.Push, "Data")]
    public class PushDataCommand : PSCmdlet
    {

        public static event DataPushed Pushed;

        [Parameter(
            Position = 0,
            Mandatory = true,
            ValueFromPipeline = true,
            HelpMessage = "The key under the current namespace which to store the value")]
        [ValidateNotNullOrEmpty]
        public string key
        {
            get; set;
        }

        [Parameter(
            Position = 1,
            Mandatory = true,
            HelpMessage = "The data to store for the associated key")]
        [ValidateNotNullOrEmpty]
        public PSObject data
        {
            get; set;
        }

        protected override void ProcessRecord()
        {
            Pushed(this);
            base.ProcessRecord();
        }
    }

    [Cmdlet(VerbsCommon.Pop, "Data")]
    public class PopDataCommand : PSCmdlet
    {

        public static event DataPopped Popped;

        [Parameter(
            Position = 0,
            Mandatory = false,
            HelpMessage = "If true flush all data, if false only flushes the last added data point.")]
        [AllowNull]
        public bool? flushAll
        {
            get; set;
        }

        protected override void ProcessRecord()
        {
            Popped(this);
            base.ProcessRecord();
        }
    }

    [Cmdlet(VerbsCommon.Clear, "Data")]
    public class DeleteCommand : PSCmdlet
    {
        
        [Parameter(
            Position = 0,
            Mandatory = true,
            HelpMessage = "The path to the key")]
        public string path
        {
            get; set;
        }

        protected override void ProcessRecord()
        {
            try {
                PSM.Store().Delete(path);
            }
            catch(Exception error)
            {
                WriteError(new ErrorRecord(error, "DELETE", ErrorCategory.NotSpecified, this));
            }
        }
    }
}
