using System.Diagnostics;
using System;
using System.Reflection;

namespace PSMonitor
{

    public delegate void Log(string msg, EventLogEntryType type);

    public static class Logger
    {       

        private static object lock0 = new object();
        
        private static string Source { get { return (Assembly.GetEntryAssembly()??Assembly.GetExecutingAssembly()).GetName().Name; } }
        private static string LogName { get { return "Application"; } }

        public static event Log OnLog;
        
        private static void Write(string msg, EventLogEntryType t)
        {
            
            string entryType = "";

            try
            {
                EventLog.WriteEntry(Source, msg, t);
            }
            catch (Exception)
            {
                EventLog.CreateEventSource(Source, LogName);
            }
            

            lock (lock0)
            {
                switch (t)
                {
                    case EventLogEntryType.Error:
                        entryType = "Error";
                        Console.ForegroundColor = ConsoleColor.Red;
                        break;
                    case EventLogEntryType.Warning:
                        entryType = "Warning";
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        break;
                    case EventLogEntryType.Information:
                        entryType = "Info";
                        Console.ForegroundColor = ConsoleColor.Green;
                        break;
                    case EventLogEntryType.SuccessAudit:
                        entryType = "Success";
                        break;
                    case EventLogEntryType.FailureAudit:
                        entryType = "Failure";
                        break;
                    default:
                        Console.ForegroundColor = ConsoleColor.White;
                        break;
                }

                if (OnLog != null) OnLog(msg, t);

                Console.WriteLine("{0} : {1} : {2}", entryType, DateTime.Now.ToString(), msg);
                Console.ResetColor();
            }
        }

        public static void Error(string msg)
        {
            Write(msg, EventLogEntryType.Error);
            Debug.WriteLine(msg);
        }

        public static void Error(Exception exc)
        {
            Write(exc.Message, EventLogEntryType.Error);
            Debug.WriteLine(exc.ToString());
        }

        public static void Warn(string msg)
        {
            Write(msg, EventLogEntryType.Warning);
            Debug.WriteLine(msg);
        }

        public static void Info(string msg)
        {
            Write(msg, EventLogEntryType.Information);
            Debug.WriteLine(msg);
        }

        public static void Success(string msg)
        {
            Write(msg, EventLogEntryType.SuccessAudit);
            Debug.WriteLine(msg);
        }

        public static void Failure(string msg)
        {
            Write(msg, EventLogEntryType.FailureAudit);
            Debug.WriteLine(msg);
        }
        
    }
}
