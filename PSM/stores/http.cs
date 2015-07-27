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

namespace PSMonitor.Stores
{

    public class HTTP : IStore
    {
        public event DataReceivedHandler DataReceived;

        private static DataContractJsonSerializer json = new DataContractJsonSerializer(typeof(Envelope[]));

        private ConcurrentQueue<Envelope> queue;
        private List<Thread> threads;
        private bool disposed = false;

        public Uri Uri { get; private set; }

        private HubConnection hub;

        public HTTP()
        {
            
            Uri = new Uri(Setup.Get<HTTP, string>("url") ?? @"http://localhost:54926/");
            queue = new ConcurrentQueue<Envelope>();

            threads = new List<Thread>();

            threads.Add(new Thread(Dispatch));
            threads.ForEach(thread => {
                thread.Name = String.Format("HTTP Store Thread #{0}", threads.IndexOf(thread));
                thread.Start(this);
            });

            hub = new HubConnection(Uri.ToString());

            hub.CreateHubProxy("DataReceivedHub").On<Envelope>("OnData",
                (data) => {

                    DataReceivedHandler handler = DataReceived;

                    if (handler != null)
                        handler(data);

                });
            
            hub.Start();
        }
        
        public long Delete(string path)
        {
            return Delete_(path, null, null);
        }
        
        public long Delete(string path, DateTime start, DateTime end)
        {
            return Delete_(path, start, end);
        }

        private long Delete_(string path, DateTime? start, DateTime? end)
        {

            long result = 0;

            using (HttpClient client = CreateClient())
            {

                
                string uri = start != null && end != null ?
                    String.Format("/data/{0}/{1}/{2}/", path, ToUnixTimestamp(start.Value) * 1000, ToUnixTimestamp(end.Value) * 1000) :
                    String.Format("/data/{0}/", path);

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

        public void Dispose()
        {
            hub.Dispose();

            disposed = true;
            threads.ForEach(thread => {
                thread.Interrupt();
                Debug.WriteLine("HTTP Store : Waiting for threads to exit");
                thread.Join();
            });
        }

        public Entry Get(string path)
        {

            foreach(Entry entry in Get_(path, null, null)) {
                return entry;
            }

            throw new KeyNotFoundException("Could not find the specified key or path");

        }

        private static DateTime t = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        public static DateTime FromUnixTimestamp(long seconds)
        {            
            return t.AddSeconds(seconds).ToLocalTime();
        }

        public static long ToUnixTimestamp(DateTime time)
        {
            return (long)time.ToUniversalTime().Subtract(t).TotalSeconds;
        }
        
        public IEnumerable<Entry> Get(string path, DateTime start, DateTime end)
        {
            return Get_(path, start, end);
        }

        public IEnumerable<Entry> Get(string path, long start, long end)
        {
            return Get_(path, start, end);
        }

        private IEnumerable<Entry> Get_(string path, object start, object end)
        {

            DataContractJsonSerializer json = new DataContractJsonSerializer(typeof(Entry[]));
            Entry[] entries                 = new Entry[0];

            using (HttpClient client = CreateClient())
            {

                string uri = start != null && end != null && typeof(DateTime).Equals(start.GetType()) && typeof(DateTime).Equals(end.GetType()) ?
                    String.Format("/data/{0}/{1}/{2}/time", path, ToUnixTimestamp((DateTime)start) * 1000, ToUnixTimestamp((DateTime)end) * 1000) : start != null && end != null && typeof(long).Equals(start.GetType()) && typeof(long).Equals(end.GetType()) ?
                    String.Format("/data/{0}/{1}/{2}/index", path, (long)start, (long)end) :
                    String.Format("/data/{0}/", path);

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

        private HttpClient CreateClient()
        {

            HttpClient client  = new HttpClient();

            client.Timeout     = new TimeSpan(0, 0, 0, PSMonitor.Setup.Master.defaultTimeout);
            client.BaseAddress = Uri;

            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));            

            return client;

        }

        public void Put(Envelope data)
        {
            if(!disposed)
                queue.Enqueue(data);
        }

        private int sleepTime = 1000;
        
        private static void Dispatch(object ctx)
        {

            HTTP context        = (HTTP)ctx;
            HttpClient client   = context.CreateClient();           

            {

                while(!context.disposed)
                {

                    try
                    {
                        Thread.Sleep(context.sleepTime);
                    }
                    catch (ThreadInterruptedException e)
                    {
                        Debug.WriteLine(e.ToString());
                    }

                    if (!context.queue.IsEmpty) {


                        using (MemoryStream stream = new MemoryStream())
                        {

                            Envelope[] data = context.queue.ToArray();
                           
                            json.WriteObject(stream, data);
                            stream.Seek(0, SeekOrigin.Begin);

                            StreamContent content = new StreamContent(stream);

                            content.Headers.Add("Content-Type", "application/json");

                            client.PutAsync("/data", content).ContinueWith(task =>
                            {

                                if (task.Status != TaskStatus.RanToCompletion)
                                {

                                    Debug.WriteLine("HTTP::Dispatch - Task did not run to completion. Status = {0}", Convert.ToString(task.Status)??"null");

                                    context.sleepTime = Math.Max(context.sleepTime * 2, 1000 * 60 * 60 * 24);

                                    foreach (Envelope envelope in data)
                                    {
                                        envelope.Retry = envelope.Retry == 0 ? 2 : envelope.Retry;
                                    }

                                    if (task.Exception != null)
                                    {

                                        Logger.error(task.Exception);

                                        foreach (Exception e in task.Exception.InnerExceptions)
                                        {
                                            Logger.error(e);
                                        }

                                    }

                                }
                                else if (task.Result != null)
                                {

                                    HttpResponseMessage response = task.Result;
                                    
                                    
                                    if (!response.IsSuccessStatusCode)
                                        Logger.failure(String.Format("Server responded with {0} {1}", response.StatusCode, response.ReasonPhrase));

                                    context.sleepTime = 1000;

                                }

                                for (int i = 0; i < data.Length; i++)
                                {

                                    Envelope envelope;

                                    while (!context.queue.TryDequeue(out envelope)) ;

                                    if (--envelope.Retry > 0)
                                    {
                                        context.queue.Enqueue(envelope);
                                    }

                                }

                            }).Wait();
                        }

                    }
                    
                }

                client.Dispose();
               
            }

        }

        public Key[] GetKeys(string path)
        {

            DataContractJsonSerializer json = new DataContractJsonSerializer(typeof(Key[]));
            Key[]                      keys = new Key[0];

            using (HttpClient client = CreateClient())
            {

                string uri =  path.Length == 0 ? "/keys/" : String.Format("/keys/{0}/", path);

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

