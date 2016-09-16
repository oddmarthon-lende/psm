/// <copyright file="log.xaml.cs" company="Baker Hughes Incorporated">
/// Copyright (c) 2015 All Rights Reserved
/// </copyright>
/// <author>Odd Marthon Lende</author>

using System.Collections.ObjectModel;
using System.Windows;
using System.Diagnostics;
using System;
using System.Windows.Threading;

namespace PSMViewer
{
    /// <summary>
    /// Interaction logic for log.xaml
    /// </summary>
    public partial class EventLogWindow : PSMonitor.Theme.Window
    {
        /// <summary>
        /// Event log entry data structure
        /// </summary>
        public class Event
        {

            public string Time { get; }

            /// <summary>
            /// Entry type
            /// </summary>
            public EventLogEntryType Type { get; }

            /// <summary>
            /// Entry Message
            /// </summary>
            public string Message { get; }

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="msg"></param>
            /// <param name="type"></param>
            public Event(string msg, EventLogEntryType type)
            {

                this.Time = DateTime.Now.ToString();
                this.Type = type;
                this.Message = msg;

            }
        }

        /// <summary>
        /// Holds the event log entries
        /// </summary>
       public ObservableCollection<Event> Events { get; set; } = new ObservableCollection<Event>();

        /// <summary>
        /// Constructor
        /// </summary>
        public EventLogWindow()
        {

            InitializeComponent();

            Dispatcher d = System.Windows.Threading.Dispatcher.CurrentDispatcher;

            PSMonitor.Logger.OnLog += (msg, eventType) => d.Invoke(delegate { Events.Add(new Event(msg, eventType)); });
           
        }

        /// <summary>
        /// Clears the log
        /// </summary>
        public void Clear(object sender = null, RoutedEventArgs e = null)
        {
            Events.Clear();
        }
    }
}
