/// <copyright file="keysController.cs" company="Baker Hughes Incorporated">
/// Copyright (c) 2015 All Rights Reserved
/// </copyright>
/// <author>Odd Marthon Lende</author>
/// <summary>HTTP Controller for loading keys</summary>
/// 

using System.Collections.Generic;
using System.Web.Http;
using PSM.Stores;

namespace PSM.HttpApi.Controllers
{
    /// <summary>
    /// A controller that loads the keys from the store
    /// </summary>
    public class keysController : ApiController
    {
        
        /// <summary>
        /// Gets the keys from the store
        /// </summary>
        /// <param name="path">The namespace</param>
        /// <returns>The keys enumerable</returns>
        private IEnumerable<Key> GetKeys(string path)
        {
            return Store.GetKeys(path ?? "");
        }

        /// <summary>
        /// Gets the keys in the root.
        /// </summary>
        /// <returns>The keys enumerable</returns>
        [HttpGet]
        public IEnumerable<Key> Get()
        {
            return GetKeys(null);
        }

        /// <summary>
        /// Gets the keys for the <paramref name="path"/>
        /// </summary>
        /// <param name="path">The namespace path</param>
        /// <returns>The keys enumerable</returns>
        [HttpGet]
        public IEnumerable<Key> Get(string path)
        {
            return GetKeys(path);
        }
        
    }
}
