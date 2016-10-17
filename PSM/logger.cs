/// <copyright file="logger.cs" company="Baker Hughes Incorporated">
/// Copyright (c) 2015 All Rights Reserved
/// </copyright>
/// <author>Odd Marthon Lende</author>
/// 
using System.Diagnostics;
using System;
using System.Reflection;

namespace PSM
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

            //try
            //{
            //    if(t == EventLogEntryType.Error)
            //        EventLog.WriteEntry(Source, msg, t);
            //}
            //catch (Exception)
            //{
            //    try
            //    {
            //        EventLog.CreateEventSource(Source, LogName);
            //    }
            //    catch(Exception) { }

            //}
            

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

                Console.WriteLine("{0} : {1} : {2}", entryType, DateTime.Now.ToString("o"), msg);
                Console.ResetColor();
            }

            if (OnLog != null) OnLog(msg, t);
        }

        public static void Error(string msg)
        {
            Write(msg, EventLogEntryType.Error);
            
        }

        public static void Error(Exception exc)
        {
            Write(exc.Message, EventLogEntryType.Error);
            
        }

        public static void Warn(string msg)
        {
            Write(msg, EventLogEntryType.Warning);
            
        }

        public static void Info(string msg)
        {
            Write(msg, EventLogEntryType.Information);
            
        }

        public static void Success(string msg)
        {
            Write(msg, EventLogEntryType.SuccessAudit);
            
        }

        public static void Failure(string msg)
        {
            Write(msg, EventLogEntryType.FailureAudit);
            
        }
        
    }
}
