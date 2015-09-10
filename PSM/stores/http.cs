/// <copyright file="http.cs" company="Baker Hughes Incorporated">
/// Copyright (c) 2015 All Rights Reserved
/// </copyright>
/// <author>Odd Marthon Lende</author>
/// <summary>The HTTP store</summary>
/// 
using System;
using System.Threading;
using System.Net.Http;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNet.SignalR.Client;
using System.Web;
using System.Linq;
using System.ComponentModel;

namespace PSMonitor.Stores
{
    /// <summary>
    /// A store that connects over HTTP
    /// </summary>
    public class HTTP : Store
    {


        public class Configuration
        {

            [Description("The url used to connect to the server")]
            public string Url
            {
                get
                {
                    return Setup.Get<HTTP, string>("url");
                }

                set
                {

                }
            }

            [Description("The number of milliseconds to wait before raising an error when connecting to the server")]
            public int ConnectionTimeout
            {

                get
                {
                    return PSMonitor.Setup.Master.defaultTimeout;
                }

                set
                {

                }

            }
        }

        /// <summary>
        /// The serializer used to convert data to JSON
        /// </summary>
        private static DataContractJsonSerializer json = new DataContractJsonSerializer(typeof(Envelope[]));

        /// <summary>
        /// Queue of data waiting to be sent over the http connection
        /// </summary>
        private ConcurrentQueue<Envelope> _dispatch_queue = new ConcurrentQueue<Envelope>();

        /// <summary>
        /// The threads created by this instance
        /// </summary>
        private IReadOnlyCollection<Thread> _threads;

        /// <summary>
        /// Set to <c>true</c> when this instance is disposed,
        /// </summary>
        private bool _disposed = false;

        /// <summary>
        /// The url to connect to
        /// </summary>
        private Uri _uri;

        /// <summary>
        /// The Signalr hub connection
        /// </summary>
        private HubConnection _hub;

        /// <summary>
        /// The sleeptime for the threads
        /// </summary>
        private int _sleepTime = 1000;

        /// <summary>
        /// Just a unique identifier used to name the threads
        /// </summary>
        private Guid _id = Guid.NewGuid();

        /// <summary>
        /// Constructor
        /// </summary>
        public HTTP()
        {

            Options = new Configuration();

            _uri = new Uri(((Configuration)Options).Url ?? @"http://localhost:54926/");

            List<Thread> threads = new List<Thread>();

            threads.Add(new Thread(Dispatch));
            threads.ForEach(thread => {
                thread.Name = String.Format("HTTP Store [{0}] Thread #{1}", _id, threads.IndexOf(thread));
                thread.Start(this);
            });

            _threads = threads;
                       
        }
        
        /// <summary>
        /// Called when new data arrives through the hub.
        /// </summary>
        /// <param name="data">The data envelope containing metadata and new data entries.</param>
        private void Received(Envelope data)
        {

            Debug.Assert(data != null && data.Entries.Length > 0);
            Debug.WriteLine("PSMonitor.Stores.HTTP.Received(Envelope data) : Received data '{0}', Count: {1}", data.Entries[0], data.Entries.Length);

            foreach (KeyValuePair<object, ConcurrentBag<Store.Path>> pair in Receivers)
            {

                ConcurrentBag<Store.Path> bag = pair.Value;
                Envelope result;

                foreach(Path path in bag)
                {

                    if(data.Path == path.Namespace)
                    {
                        // Requery the data and filter out any entries that does not match the criteria.
                        Entry[] entries = (from entry in data.Entries where (entry.Timestamp > (DateTime)path.StartIndex && entry.Key == path.Key) select entry).ToArray();

                        if (entries.Length > 0)
                        {
                            // Make a copy
                            result = new Envelope(data) { Entries = entries };

                            // Set the start index with the result from the handler.
                            path.StartIndex = path.Handler(result);
                        }

                    }
                }
            }
        }

        /// <summary>
        /// <see cref="IStore.Delete(string)"/>
        /// </summary>
        public override long Delete(string path)
        {
            return Delete_(path, null, null);
        }

        /// <summary>
        /// <see cref="IStore.Delete(string, DateTime, DateTime)"/>
        /// </summary>
        public override long Delete(string path, DateTime start, DateTime end)
        {
            return Delete_(path, start, end);
        }

        /// <summary>
        /// Can handle both methods above with null values.
        /// <see cref="Delete(string)"/>
        /// <see cref="Delete(string, DateTime, DateTime)"/>
        /// </summary>
        private long Delete_(string path, DateTime? start, DateTime? end)
        {

            long result = 0;

            using (HttpClient client = CreateClient())
            {

                
                string uri = HttpUtility.UrlEncode(start != null && end != null ?
                    String.Format("/data/{0}/{1}/{2}/", path, ToUnixTimestamp(start.Value) * 1000, ToUnixTimestamp(end.Value) * 1000) :
                    String.Format("/data/{0}/", path));
                
                client.DeleteAsync(uri).ContinueWith(async task =>
                {

                    if (task.Status != TaskStatus.RanToCompletion)
                    {

                        if (task.Exception != null)
                        {
                            throw task.Exception;
                        }

                    }
                    else if (task.Result != null)
                    {

                        using (HttpResponseMessage response = task.Result)
                        {
                            response.EnsureSuccessStatusCode();
                            result = Convert.ToInt64(await response.Content.ReadAsStringAsync());
                        }                            
                    }

                }).Wait();

            }

            return result;
        }

        /// <summary>
        /// Releases and cleans up any resources created.
        /// </summary>
        public override void Dispose()
        {

            _disposed = true;

            _hub.Dispose();            

            foreach(Thread thread in _threads) {
                thread.Interrupt();
                Debug.WriteLine("HTTP Store : Waiting for threads to exit");
                thread.Join();
            }

            base.Dispose();

        }

        /// <summary>
        /// <see cref="IStore.Get(string)"/>
        /// </summary>
        public override Entry Get(string path)
        {

            foreach(Entry entry in Get_(path, null, null)) {
                return entry;
            }

            throw new KeyNotFoundException("Could not find the specified key or path");

        }

        private static DateTime t = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// Converts unix timestamp to <see cref="DateTime"/>
        /// </summary>
        /// <param name="seconds">The unix timestamp</param>
        /// <returns>A new <see cref="DateTime"/> object</returns>
        public static DateTime FromUnixTimestamp(long seconds)
        {            
            return t.AddSeconds(seconds).ToLocalTime();
        }

        /// <summary>
        /// Converts a <see cref="DateTime"/> object to a unix timestamp
        /// </summary>
        /// <param name="time">The <see cref="DateTime"/> object to convert</param>
        /// <returns>The unix timestamp</returns>
        public static long ToUnixTimestamp(DateTime time)
        {
            return (long)time.ToUniversalTime().Subtract(t).TotalSeconds;
        }

        /// <summary>
        /// <see cref="IStore.Get(string, DateTime, DateTime)"/>
        /// </summary>
        public override IEnumerable<Entry> Get(string path, DateTime start, DateTime end)
        {
            return Get_(path, start, end);
        }

        /// <summary>
        /// <see cref="IStore.Get(string, long, long)"/>
        /// </summary>
        public override IEnumerable<Entry> Get(string path, long start, long end)
        {
            return Get_(path, start, end);
        }

        /// <summary>
        /// Handles both methods above
        /// <see cref="Get(string, DateTime, DateTime)"/>
        /// <see cref="Get(string, long, long)"/>
        /// </summary>
        private IEnumerable<Entry> Get_(string path, object start, object end)
        {
            
            DataContractJsonSerializer json = new DataContractJsonSerializer(typeof(Entry[]));
            Entry[] entries                 = new Entry[0];

            using (HttpClient client = CreateClient())
            {
                
                string uri = HttpUtility.UrlEncode(start != null && end != null && typeof(DateTime).Equals(start.GetType()) && typeof(DateTime).Equals(end.GetType()) ?
                    String.Format("/data/{0}/{1}/{2}/time", path, ToUnixTimestamp((DateTime)start) * 1000, ToUnixTimestamp((DateTime)end) * 1000) : start != null && end != null && typeof(long).Equals(start.GetType()) && typeof(long).Equals(end.GetType()) ?
                    String.Format("/data/{0}/{1}/{2}/index", path, (long)start, (long)end) :
                    String.Format("/data/{0}/", path));

                
                client.GetStreamAsync(uri).ContinueWith(task =>
                {
                    
                    if (task.Status != TaskStatus.RanToCompletion)
                    {
                        
                        if (task.Exception != null)
                        {
                            throw task.Exception;
                        }

                    }
                    else if (task.Result != null)
                    {
                        
                        entries = (Entry[])json.ReadObject(task.Result);

                    }

                }).Wait();

            }

            return entries;

        }

        /// <summary>
        /// Creates a new http connection
        /// </summary>
        /// <returns>A new client</returns>
        private HttpClient CreateClient()
        {

            HttpClient client  = new HttpClient();

            client.Timeout     = new TimeSpan(0, 0, 0, ((Configuration)Options).ConnectionTimeout);
            client.BaseAddress = _uri;

            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            
            if (_hub == null)
            {

                _hub = new HubConnection(_uri.ToString());
                _hub.CreateHubProxy("RealTimeDataHub").On<Envelope>("OnData", Received);

            }

            if (_hub.State == ConnectionState.Disconnected)
            {
                Debug.WriteLine("HTTP.CreateClient(): Connecting to SignalR hub");
                _hub.Start();                
            }

            DateTime start = DateTime.Now;

            while (_hub.State != ConnectionState.Connected)
            {

                Debug.WriteLine("HTTP.CreateClient(): Waiting for connection...");

                Thread.Sleep(100);

                if (_hub.State == ConnectionState.Connected)
                    Debug.WriteLine("HTTP.CreateClient(): Connected to SignalR hub. Connection ID is: {0}", (object)_hub.ConnectionId);
                else if ((DateTime.Now - start).TotalMilliseconds > ((Configuration)Options).ConnectionTimeout)
                    throw new TimeoutException("Timeout exceeded. Could not connect to server");
            }            

            client.DefaultRequestHeaders.Add("ConnectionId", _hub.ConnectionId);

            return client;

        }

        /// <summary>
        /// <see cref="IStore.Put(Envelope)"/>
        /// </summary>
        public override void Put(Envelope data)
        {
            if(!_disposed)
                _dispatch_queue.Enqueue(data);
        }
        
        /// <summary>
        /// Entry point for the thread that dispatches data.
        /// </summary>
        /// <param name="ctx">The <see cref="HTTP"/>instance</param>
        private static void Dispatch(object ctx)
        {

            HTTP context = (HTTP)ctx;

            try {

                HttpClient client   = context.CreateClient();

                while(!context._disposed)
                {

                    Thread.Sleep(context._sleepTime);                                      

                    if (!context._dispatch_queue.IsEmpty) {


                        using (MemoryStream stream = new MemoryStream())
                        {

                            Envelope[] data = context._dispatch_queue.ToArray();
                           
                            json.WriteObject(stream, data);
                            stream.Seek(0, SeekOrigin.Begin);

                            StreamContent content = new StreamContent(stream);

                            content.Headers.Add("Content-Type", "application/json");

                            client.PutAsync("/data", content).ContinueWith(task =>
                            {

                                if (task.Status != TaskStatus.RanToCompletion)
                                {

                                    Debug.WriteLine("HTTP::Dispatch - Task did not run to completion. Status = {0}", Convert.ToString(task.Status)??"null");

                                    context._sleepTime = Math.Max(context._sleepTime * 2, 1000 * 60 * 60 * 24);

                                    foreach (Envelope envelope in data)
                                    {
                                        envelope.Retry = envelope.Retry == 0 ? 2 : envelope.Retry;
                                    }

                                    if (task.Exception != null)
                                    {

                                        Logger.Error(task.Exception);

                                        foreach (Exception e in task.Exception.InnerExceptions)
                                        {
                                            Logger.Error(e);
                                        }

                                    }

                                }
                                else if (task.Result != null)
                                {

                                    HttpResponseMessage response = task.Result;
                                    
                                    
                                    if (!response.IsSuccessStatusCode)
                                        Logger.Failure(String.Format("Server responded with {0} {1}", response.StatusCode, response.ReasonPhrase));

                                    context._sleepTime = 1000;

                                }

                                for (int i = 0; i < data.Length; i++)
                                {

                                    Envelope envelope;

                                    while (!context._dispatch_queue.TryDequeue(out envelope)) ;

                                    if (--envelope.Retry > 0)
                                    {
                                        context._dispatch_queue.Enqueue(envelope);
                                    }

                                }

                            }).Wait();
                        }

                    }
                    
                }

                client.Dispose();
               
            }
            catch (ThreadInterruptedException e)
            {
                Debug.WriteLine(e.ToString());
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

        }
        
        /// <summary>
        /// <see cref="IStore.GetKeys(string)"/>
        /// </summary>
        public override Key[] GetKeys(string path)
        {

            DataContractJsonSerializer json = new DataContractJsonSerializer(typeof(Key[]));
            Key[]                      keys = new Key[0];

            using (HttpClient client = CreateClient())
            {

                string uri = HttpUtility.UrlEncode(path.Length == 0 ? "/keys/" : String.Format("/keys/{0}/", path));

                client.GetStreamAsync(uri).ContinueWith(task =>
                {

                    if (task.Status != TaskStatus.RanToCompletion)
                    {

                        if (task.Exception != null)
                        {
                            throw task.Exception;
                        }

                    }
                    else if (task.Result != null)
                    {
                        keys = (Key[])json.ReadObject(task.Result);
                    }

                }).Wait();

            }

            return keys;
        }

        
    }

}

