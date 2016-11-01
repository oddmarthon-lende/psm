/// <copyright file="commands.cs" company="Baker Hughes Incorporated">
/// Copyright (c) 2015 All Rights Reserved
/// </copyright>
/// <author>Odd Marthon Lende</author>
///
using System;
using System.Management.Automation;

namespace PSM.Powershell
{

    internal delegate void IntervalChanged(SetIntervalCommand sender);
    internal delegate void NamespaceChanged(SetNamespaceCommand sender);
    internal delegate void DataPushed(PushDataCommand sender);

    [Cmdlet(VerbsCommon.Set, "Interval")]
    internal class SetIntervalCommand : PSCmdlet
    {

        public static event IntervalChanged Changed;
        
        [Parameter(
            Position = 0,
            Mandatory = true,
            HelpMessage = "Sets the interval the script will run in milliseconds")]
        [ValidateNotNullOrEmpty]
        public double Interval
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
    internal class SetNamespaceCommand : PSCmdlet
    {

        public static event NamespaceChanged Changed;

        [Parameter(
            Position = 0,
            Mandatory = true,
            HelpMessage = "Sets the current namespace for the data output")]
        [ValidateNotNullOrEmpty]
        public string Path
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
    internal class PushDataCommand : PSCmdlet
    {

        public static event DataPushed Pushed;

        [Parameter(
            Position = 0,
            Mandatory = true,
            ValueFromPipeline = true,
            HelpMessage = "The key under the current namespace which to store the value")]
        [ValidateNotNullOrEmpty]
        public string Key
        {
            get; set;
        }

        [Parameter(
            Position = 1,
            Mandatory = true,
            HelpMessage = "The data to store for the associated key")]
        public PSObject Data
        {
            get; set;
        }

        [Parameter(
            Position = 2,
            Mandatory = false,
            HelpMessage = "An optional timestamp")]
        public DateTime? Timestamp
        {
            get; set;
        }

        protected override void ProcessRecord()
        {
            if (Data == null)
                return; 
            
            Pushed(this);
            base.ProcessRecord();
        }
    }
        
}
