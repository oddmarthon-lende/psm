using System.Diagnostics;
using System;
using System.Reflection;

namespace PSMonitor
{

    public delegate void Log(string msg, EventLogEntryType type);

    public static class Logger
    {       

        private static object lock0 = new object();

        private static string s { get { return Assembly.GetExecutingAssembly().FullName; } }
        private static string l { get { return "Application"; } }

        public static event Log OnLog;
        
        private static void write(string msg, EventLogEntryType t)
        {

            string entryType = "";

            if (!EventLog.SourceExists(s))
                EventLog.CreateEventSource(s, l);

            EventLog.WriteEntry(s, msg, t);

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

        public static void error(string msg)
        {
            write(msg, EventLogEntryType.Error);
            Debug.WriteLine(msg);
        }

        public static void error(Exception exc)
        {
            write(exc.Message, EventLogEntryType.Error);
            Debug.WriteLine(exc.ToString());
        }

        public static void warn(string msg)
        {
            write(msg, EventLogEntryType.Warning);
            Debug.WriteLine(msg);
        }

        public static void info(string msg)
        {
            write(msg, EventLogEntryType.Information);
            Debug.WriteLine(msg);
        }

        public static void success(string msg)
        {
            write(msg, EventLogEntryType.SuccessAudit);
            Debug.WriteLine(msg);
        }

        public static void failure(string msg)
        {
            write(msg, EventLogEntryType.FailureAudit);
            Debug.WriteLine(msg);
        }
        
    }
}
