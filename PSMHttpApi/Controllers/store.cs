﻿/// <copyright file="store.cs" company="Baker Hughes Incorporated">
/// Copyright (c) 2015 All Rights Reserved
/// </copyright>
/// <author>Odd Marthon Lende</author>
/// <summary>Store stuff</summary>
/// 

using Microsoft.AspNet.SignalR;
using PSMonitor.Stores;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web.Http.Controllers;

namespace PSMonitor.Controllers
{

    /// <summary>
    /// A class that communicates with the store and handles realtime data forwarding to clients
    /// </summary>
    public static class Store
    {

        /// <summary>
        /// All <see cref="Receiver"/>'s that has been registered with the store
        /// </summary>
        private static ConcurrentStack<object> _receivers = new ConcurrentStack<object>();

        /// <summary>
        /// The receiver context
        /// </summary>
        private class Receiver
        {

            /// <summary>
            /// Reference to the SignalR hub
            /// </summary>
            private static IHubContext _hub = GlobalHost.ConnectionManager.GetHubContext<RealTimeDataHub>();

            /// <summary>
            /// The the SignalR connection id.
            /// </summary>
            public string ConnectionId { get; private set; }

            /// <summary>
            /// The last index. Only data with a higher index is sent.
            /// </summary>
            public object Index { get; private set; }

            /// <summary>
            /// The data path
            /// </summary>
            public string Path { get; private set; }

            /// <summary>
            /// The <see cref="RealTimeData"/> handler. Called by the store when there is new data.
            /// </summary>
            /// <param name="data"></param>
            /// <returns></returns>
            public object Handler(Envelope data)
            {
                
                dynamic client  = _hub.Clients.Client(ConnectionId);
                
                if(client != null)
                {
                    Debug.WriteLine("PSMonitor.Controllers.Store.Receiver.Handler(Envelope data): Sending data {0}, Count : {1}", Path, data.Entries.Length);
                    client.OnData(data);
                }
                else
                {
                    Unregister(this);
                }

                Index = data.Entries.Max((entry) => { return entry.Timestamp; });

                return Index;
            }

            /// <summary>
            /// The hashcode used for comparing object equality in collections etc.
            /// </summary>
            private int _hashCode = 0;

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="Context">The context of the currently executing HTTP action</param>
            /// <param name="Path">The data path</param>
            /// <param name="StartingIndex">The index from which to begin loading data.</param>
            public Receiver(HttpActionContext Context, string Path, object StartingIndex) : this(GetConnectionId(Context), Path, StartingIndex)
            { }

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="ConnectionId">The SignalR connection Id</param>
            /// <param name="Path">The data path</param>
            /// <param name="StartingIndex">The index from which to begin loading data.</param>
            public Receiver(string ConnectionId, string Path, object StartingIndex)
            {

                if (ConnectionId == null)
                    throw new ArgumentNullException("ConnectionId cannot be null");

                if (StartingIndex == null)
                    throw new ArgumentNullException("StartingIndex cannot be null");

                if (Path == null)
                    throw new ArgumentNullException("Path cannot be null");

                this.ConnectionId = ConnectionId;
                this.Index = StartingIndex;
                this.Path = Path;

                _hashCode = ToString().GetHashCode();

            }

            /// <summary>
            /// Overrides <see cref="object.ToString"/>
            /// </summary>
            /// <returns>A string representation of this instance</returns>
            public override string ToString()
            {
                return String.Format("{0}:{1}", ConnectionId, Path);
            }

            /// <summary>
            /// Overloads Equality operator
            /// </summary>
            /// <param name="r1">First instance</param>
            /// <param name="r2">Second instance</param>
            /// <returns><c>true</c> if equal</returns>
            public static bool operator == (Receiver r1, Receiver r2)
            {
                return r1.Equals(r2);
            }

            /// <summary>
            /// Overloads inequality operator
            /// </summary>
            /// <param name="r1">First instance</param>
            /// <param name="r2">Second instance</param>
            /// <returns><c>true</c> if not equal</returns>
            public static bool operator !=(Receiver r1, Receiver r2)
            {
                return !r1.Equals(r2);
            }

            /// <summary>
            /// Overrides the <see cref="object.Equals(object)"/>
            /// </summary>
            /// <param name="obj"></param>
            /// <returns><c>true</c> if equal</returns>
            public override bool Equals(object obj)
            {

                if (obj is Receiver)
                    return ((Receiver)obj).GetHashCode() == GetHashCode();

                return false;
            }

            public override int GetHashCode()
            {
                return _hashCode;
            }

        }

        /// <summary>
        /// Gets the SignalR connection id from the HTTP headers.
        /// This header is set by the <see cref="HTTP"/> store.
        /// </summary>
        /// <param name="context">The currently executing HTTP action context</param>
        /// <returns>The connection id</returns>
        private static string GetConnectionId(HttpActionContext context)
        {
            return context.Request.Headers.GetValues("ConnectionId").First();
        }

        /// <summary>
        /// <see cref="IStore.Delete(string)"/>
        /// </summary>
        /// <param name="path">The data path</param>
        /// <param name="context">The currently executing HTTP action context</param>
        /// <returns><see cref="IStore.Delete(string)"/></returns>
        public static long Delete(string path, HttpActionContext context)
        {
            Unregister(GetConnectionId(context));
            return PSM.Store().Delete(path);
        }

        /// <summary>
        /// <see cref="IStore.Delete(string, DateTime, DateTime)"/>
        /// </summary>
        /// <param name="path">The data path</param>
        /// <param name="start">The start time index</param>
        /// <param name="end">The end time index</param>
        /// <param name="context">The currently executing HTTP action context</param>
        /// <returns><see cref="IStore.Delete(string, DateTime, DateTime)"/></returns>
        public static long Delete(string path, DateTime start, DateTime end, HttpActionContext context)
        {
            return PSM.Store().Delete(path, start, end);
        }

        /// <summary>
        /// <see cref="IStore.Get(string)"/>
        /// </summary>
        /// <param name="path">The data path</param>
        /// <param name="context">The currently executing HTTP action context</param>
        /// <returns><see cref="IStore.Get(string)"/></returns>
        public static Entry Get(string path, HttpActionContext context)
        {

            Entry entry = PSM.Store().Get(path);
            Receiver receiver = new Receiver(context, path, entry.Timestamp);

            if (!_receivers.Contains(receiver))
            {                
                Register(receiver, path, receiver.Index, receiver.Handler);
            }                     

            return entry;
        }

        /// <summary>
        /// <see cref="IStore.Get(string, long, long)"/>
        /// </summary>
        /// <param name="path">The data path</param>
        /// <param name="start">The start index</param>
        /// <param name="end">The end index</param>
        /// <param name="context">The currently executing HTTP action context</param>
        /// <returns><see cref="IStore.Get(string, long, long)"/></returns>
        public static IEnumerable<Entry> Get(string path, long start, long end, HttpActionContext context)
        {
                        
            Receiver receiver = new Receiver(context, path, PSM.Store().Get(path).Timestamp);
            
            if (!_receivers.Contains(receiver))
            {                
                Register(receiver, path, receiver.Index, receiver.Handler);
            }
            
            return PSM.Store().Get(path, start, end);

        }

        /// <summary>
        /// <see cref="IStore.Get(string, DateTime, DateTime)"/>
        /// </summary>
        /// <param name="path">The data path</param>
        /// <param name="start">The start time index</param>
        /// <param name="end">The end time index</param>
        /// <param name="context"></param>
        /// <returns>The currently executing HTTP action context</returns>
        public static IEnumerable<Entry> Get(string path, DateTime start, DateTime end, HttpActionContext context)
        {

            Receiver receiver = new Receiver(context, path, PSM.Store().Get(path).Timestamp);

            if (!_receivers.Contains(receiver))
            {                
                Register(receiver, path, receiver.Index, receiver.Handler);
            }                     

            return PSM.Store().Get(path, start, end);
        }

        /// <summary>
        /// <see cref="IStore.GetKeys(string)"/>
        /// </summary>
        /// <param name="path">The namespace path</param>
        /// <returns><see cref="IStore.GetKeys(string)"/></returns>
        public static Key[] GetKeys(string path)
        {
            return PSM.Store().GetKeys(path);
        }

        /// <summary>
        /// <see cref="IStore.Put(Envelope)"/>
        /// </summary>
        /// <param name="envelope">The data</param>
        public static void Put(Envelope envelope)
        {
            PSM.Store().Put(envelope);
        }

        /// <summary>
        /// <see cref="IStore.Register(object, string, object, RealTimeData)"/>
        /// Also adds the <paramref name="context"/> to the <see cref="_receivers"/> stack.
        /// </summary>
        private static void Register(object context, string path, object startingIndex, RealTimeData handler)
        {
            
            PSM.Store().Register(context, path, startingIndex, handler);

            if(!_receivers.Contains(context))
                _receivers.Push(context);

        }

        /// <summary>
        /// <see cref="IStore.Unregister(object)"/>
        /// Also removes the <paramref name="context"/> from <see cref="_receivers"/>
        /// </summary>
        public static void Unregister(object context)
        {

            PSM.Store().Unregister(context);

            if(_receivers.Contains(context))
            {

                int count = _receivers.Count;
                object receiver;

                while (count >= 0)
                {

                    while (!_receivers.TryPop(out receiver));

                    if (receiver != context)
                        _receivers.Push(receiver);
                    else
                    {
                        break;
                    }

                    count--;

                }
                                
            }
                

        }

        /// <summary>
        /// Unregisters all <see cref="Receiver"/>'s that has the provided <paramref name="ConnectionId"/>
        /// </summary>
        /// <param name="ConnectionId">The connection id</param>
        public static void Unregister(string ConnectionId)
        {
                        
            foreach(Receiver receiver in _receivers.ToArray())
            {

                if (receiver.ConnectionId == ConnectionId)
                    Unregister(receiver);

            }

        }
    }
}