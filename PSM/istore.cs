/// <copyright file="istore.cs" company="Baker Hughes Incorporated">
/// Copyright (c) 2015 All Rights Reserved
/// </copyright>
/// <author>Odd Marthon Lende</author>
/// <summary>Store interface class declarations\specifications</summary>
/// 
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ServiceModel;

namespace PSM.Stores
{


    /// <summary>
    /// A class that holds the <see cref="PropertyDescriptor"/>'s for the properties in a type that implements <see cref="IOptions"/> interface and a dictionary that can be filled with valid values for a property.
    /// </summary>
    public class Properties : Dictionary<PropertyDescriptor, List<KeyValuePair<object, object>>> { };
    
    /// <summary>
    /// Interface for accessing options and get valid choice values for each option.
    /// </summary>
    public interface IOptions
    {
        
        /// <summary>
        /// Get the properties as a <see cref="Properties"/> object.
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        Properties Get();

        /// <summary>
        /// Gets the property value
        /// </summary>
        /// <typeparam name="T">The property value type</typeparam>
        /// <param name="PropertyName">The property name</param>
        /// <returns>The value</returns>
        T Get<T>(string PropertyName);

    }
    
    /// <summary>
    /// Defines the interface that is used when accessing the data store.
    /// <typeparam name="T">The type </typeparam>
    /// </summary>
    [ServiceContract(Namespace = "http://www.bakerhughes.com/psm/store")]
    public interface IStore : IDisposable
    {

        /// <summary>
        /// The default index field
        /// </summary>
        Enum Default { [OperationContract] get; }
        
        /// <summary>
        /// The enum type with index identifiers
        /// </summary>
        Type Index { [OperationContract] get; }
                
        /// <summary>
        /// Gets an object that contains properties that can be used to configure the store and be exposed to the user.
        /// </summary>
        IOptions Options { get; }

        /// <summary>
        /// Should output data here after it is stored in the interface implementation, so that it can be fetched and piped into another object.
        /// </summary>
        event RealTimeData Output;

        /// <summary>
        /// Get data
        /// </summary>
        /// <param name="path">The key path</param>
        /// <param name="start">The start index</param>
        /// <param name="end">The end index</param>
        /// <param name="index">The index identifier</param>
        /// <returns>An <see cref="IEnumerable{Entry}"/> of the data</returns>
        [OperationContract]
        IEnumerable<Entry> Read(string path, IComparable start, IComparable end, Enum index);

        /// <summary>
        /// Add data to the store
        /// </summary>
        /// <param name="envelope">The data <see cref="Envelope"/> that will be added. </param>
        [OperationContract]
        void Write(string path, params Entry[] entries);

        /// <summary>
        /// Delete the key and all data.
        /// </summary>
        /// <param name="path">The key path</param>
        /// <returns>The number of deleted entries</returns>
        [OperationContract]
        void Delete(string path);

        /// <summary>
        /// Get a list of keys that is contained within the provided namespace
        /// </summary>
        /// <param name="ns">The namespace</param>
        /// <returns>An array that contains the <see cref="Key"/>'s</returns>
        [OperationContract]
        IEnumerable<Key> Keys(string ns);

        /// <summary>
        /// Register for retrieval of realtime data.
        /// </summary>
        /// <param name="context">An object that will be used to identify the request.</param>
        /// <param name="path">The path to the key.</param>
        /// <param name="startingIndex">The starting index</param>
        /// <param name="indexIdentifier">The index used when fetching data</param>
        /// <param name="handler">The delegate that will receive the data <see cref="Envelope"/></param>
        [OperationContract]
        void Register(object context, string path, IComparable startingIndex, Enum indexIdentifier, RealTimeData handler);

        /// <summary>
        /// Unregister the context and stop the data transfer for all keys that was <see cref="Register(object, string, object, RealTimeData)"/>d with this context.
        /// </summary>
        /// <param name="context">The context to unregister.</param>
        [OperationContract]
        void Unregister(object context);

        /// <summary>
        /// Unregister the key for the provided context and stop the data transfer.
        /// </summary>
        /// <param name="context">The context that was used when the key was <see cref="Register(object, string, object, RealTimeData)"/></param>d.
        /// <param name="path">The key path</param>
        [OperationContract]
        void Unregister(object context, string path);

    }

    

}
