/// <copyright file="cda.cs" company="Baker Hughes Incorporated">
/// Copyright (c) 2015 All Rights Reserved
/// </copyright>
/// <author>Odd Marthon Lende</author>
using System;
using System.Runtime.InteropServices;
using System.Management.Automation;
using System.Reflection;

namespace PSMonitor
{
    [ComImport, Guid("76853DF2-E4E3-4F6B-B75C-C1DFB2FA5DF0")]
    public class MsgClient
    {
    }

    public static class CDA
    {
        private static bool disabled = false;
        private static DateTime lastConnectTime = DateTime.Now;
        private static MsgWrapper.IMsgClient client;
        public static bool isConnected { get; private set; } = false;

        public static MsgWrapper.IMsgClient Connect()
        {
            
            AssemblyName assembly = Assembly.GetExecutingAssembly().GetName();

            try {

                if(!disabled && !isConnected && DateTime.Now.Subtract(lastConnectTime).TotalMilliseconds > 60000)
                {
                    
                    if(client == null)
                        client = (MsgWrapper.IMsgClient)(new MsgClient());

                    isConnected = client.InitializeClient(assembly.Name, assembly.Version.ToString(), new { });

                    lastConnectTime = DateTime.Now;

                }
                                
            }
            catch(COMException exception)
            {

                Logger.Warn(exception.Message);

                isConnected = false;
                disabled    = true;

            }

            return client;
        }

        public static void Disconnect ()
        {

            if (isConnected)
            {
                client.UninitializeClient();
                isConnected = false;
            }

        }            

    }
    
    
    
    [Cmdlet(VerbsCommon.Get, "CDA")]
    public class GetCDACommand : PSCmdlet
    {
        
        [Parameter(
            Position = 0,
            Mandatory = true,
            HelpMessage = "The mnemonic name")]
        [ValidateNotNullOrEmpty]
        public string mnemonic
        {
            get; set;
        }

        protected override void ProcessRecord()
        {

            MsgWrapper.IMsgClient cda = CDA.Connect();

            if (CDA.isConnected)
            {

                WriteObject("");
                
            }
            else
                WriteError(new ErrorRecord(new Exception("Not connected to Advantage CDA"), "CDA_NOT_CONNECTED", ErrorCategory.ConnectionError, this));
            
        }
    }

    [Cmdlet(VerbsCommon.Set, "CDA")]
    public class SetCDACommand : PSCmdlet
    {

        [Parameter(
            Position = 0,
            Mandatory = true,
            HelpMessage = "The mnemonic name")]
        [ValidateNotNullOrEmpty]
        public string mnemonic
        {
            get; set;
        }

        [Parameter(
            Position = 1,
            Mandatory = true,
            HelpMessage = "The value to write to the mnemonic")]
        [ValidateNotNullOrEmpty]
        public object value
        {
            get; set;
        }

        protected override void ProcessRecord()
        {
            
        }
    }
}
