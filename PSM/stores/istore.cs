using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace PSMonitor.Stores
{
        
    /// <summary>
    /// Defines the interface that is used when accessing the data store.
    /// </summary>
    public interface IStore : IDisposable
    {

        /// <summary>
        /// Gets an object that contains properties that can be used to configure the store and be exposed to the user.
        /// </summary>
        object Options { get; }
        
        /// <summary>
        /// Get the current <see cref="Entry"/>
        /// </summary>
        /// <param name="path">The key path</param>
        /// <returns>The last data <see cref="Entry"/> that was written to the provided <paramref name="path"/></returns>
        Entry Get(string path);

        /// <summary>
        /// Get data
        /// </summary>
        /// <param name="path">The key path</param>
        /// <param name="start">The start time index</param>
        /// <param name="end">The end time index</param>
        /// <returns>An <see cref="IEnumerable{Entry}"/> of the data</returns>
        IEnumerable<Entry> Get(string path, DateTime start, DateTime end);

        /// <summary>
        /// Get data
        /// </summary>
        /// <param name="path">The key path</param>
        /// <param name="start">The start index</param>
        /// <param name="end">The end index</param>
        /// <returns>An <see cref="IEnumerable{Entry}"/> of the data</returns>
        IEnumerable<Entry> Get(string path, long start, long end);

        /// <summary>
        /// Add data to the store
        /// </summary>
        /// <param name="envelope">The data <see cref="Envelope"/> that will be added. </param>
        void Put(Envelope envelope);

        /// <summary>
        /// Delete the key and all data.
        /// </summary>
        /// <param name="path">The key path</param>
        /// <returns>The number of deleted entries</returns>
        long Delete(string path);

        /// <summary>
        /// Delete data within the specified <paramref name="start"/> and <paramref name="end"/> indexes.
        /// </summary>
        /// <param name="path">The key path</param>
        /// <param name="start">The start time index</param>
        /// <param name="end">The end time index</param>
        /// <returns>The number of deleted entries</returns>
        long Delete(string path, DateTime start, DateTime end);

        /// <summary>
        /// Get a list of keys that is contained within the provided namespace
        /// </summary>
        /// <param name="ns">The namespace</param>
        /// <returns>An array that contains the <see cref="Key"/>'s</returns>
        Key[] GetKeys(string ns);

        /// <summary>
        /// Register for retrieval of realtime data.
        /// </summary>
        /// <param name="context">An object that will be used to identify the request.</param>
        /// <param name="path">The path to the key.</param>
        /// <param name="startingIndex">The starting index</param>
        /// <param name="handler">The delegate that will receive the data <see cref="Envelope"/></param>
        void Register(object context, string path, object startingIndex, RealTimeData handler);

        /// <summary>
        /// Unregister the context and stop the data transfer for all keys that was <see cref="Register(object, string, object, RealTimeData)"/>d with this context.
        /// </summary>
        /// <param name="context">The context to unregister.</param>
        void Unregister(object context);

        /// <summary>
        /// Unregister the key for the provided context and stop the data transfer.
        /// </summary>
        /// <param name="context">The context that was used when the key was <see cref="Register(object, string, object, RealTimeData)"/></param>d.
        /// <param name="path">The key path</param>
        void Unregister(object context, string path);

    }

    
    /// <summary>
    /// A data model that describes a key
    /// </summary>
    /// 
    [Serializable]
    public class Key : ISerializable
    {
        /// <summary>
        /// The key name
        /// </summary>
        public string Name { get; private set; }
        /// <summary>
        /// The data type for this key's <see cref="Entry.Value"/>
        /// </summary>
        public Type Type { get; private set; }

        /// <summary>
        /// Create a new Key with the provided <paramref name="Name"/>  and <paramref name="Type"/>
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="Type"></param>
        public Key(string Name, Type Type)
        {
            this.Name = Name;
            this.Type = Type;
        }

        /// <summary>
        /// ISerializable constructor
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        public Key(SerializationInfo info, StreamingContext context)
        {
            Name = info.GetString("name");
            Type = Type.GetType(info.GetString("type"), false);
        }

        /// <summary>
        /// <see cref="ISerializable.GetObjectData(SerializationInfo, StreamingContext)"/>
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("name", Name);
            info.AddValue("type", Type != null ? Type.FullName : "");
        }
        
        /// <summary>
        /// Gets a string represention of this object.
        /// </summary>
        /// <returns>The <see cref="Name"/></returns>
        public override string ToString()
        {
            return Name;
        }
    }

}
