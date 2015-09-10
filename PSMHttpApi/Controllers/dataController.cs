﻿/// <copyright file="dataController.cs" company="Baker Hughes Incorporated">
/// Copyright (c) 2015 All Rights Reserved
/// </copyright>
/// <author>Odd Marthon Lende</author>
/// <summary>HTTP Controller that translates http request to the store interface</summary>
/// 

using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Collections.Generic;
using System;
using PSMonitor.Stores;

namespace PSMonitor.Controllers
{

    /// <summary>
    /// Loads data from the store over HTTP
    /// </summary>
    public class dataController : ApiController
    {
        /// <summary>
        /// Empty
        /// </summary>
        /// <returns>Not found</returns>
        [HttpGet]
        public IHttpActionResult Get()
        {
            return NotFound();
        }

        /// <summary>
        /// <see cref="IStore.Get(string)"/>
        /// </summary>
        [HttpGet]
        public IHttpActionResult Get(string path)
        {

            Entry entry;

            try {

                entry = Store.Get(path, ActionContext);
                

            }
            catch(KeyNotFoundException)
            {
                return NotFound();
            }
            catch (Exception error)
            {
                return InternalServerError(error);
            }

            return Json<Entry[]>(new Entry[1] { entry });

        }

        /// <summary>
        /// <see cref="IStore.Get(string, long, long)"/>
        /// <see cref="IStore.Get(string, DateTime, DateTime)"/>
        /// The <paramref name="type"/> parameter determines which of the methods above the call corresponds to
        /// </summary>
        /// <param name="path">The data path</param>
        /// <param name="start">The start index or unix timestamp</param>
        /// <param name="end">The end index or unix timestamp</param>
        /// <param name="type">The index type as a string. Can be [index, time]</param>
        /// <returns>The data as a http response</returns>
        [HttpGet]
        public IHttpActionResult Get(string path, long start, long end, string type)
        {
  
            HttpResponseMessage response  = new HttpResponseMessage(HttpStatusCode.OK);

            try {

                switch (type)
                {

                    case "index":

                        response.Content = new StreamContent(
                            new EntryJSONStream(
                                Store.Get(
                                    path,
                                    start,
                                    end,
                                    ActionContext
                            )));

                        break;

                    case "time":

                        response.Content = new StreamContent(
                            new EntryJSONStream(
                                Store.Get(
                                    path,
                                    HTTP.FromUnixTimestamp(start / 1000),
                                    HTTP.FromUnixTimestamp(end / 1000),
                                    ActionContext
                            )));

                        break;

                    default:

                        throw new ArgumentOutOfRangeException("The type argument is invalid");
                }
               
                response.Content.Headers.Add("Content-Type", "application/json");

            }
            catch(Exception error)
            {
                return InternalServerError(error);
            }

            return ResponseMessage(response);

        }

        /// <summary>
        /// <see cref="IStore.Put(Envelope)"/>
        /// </summary>
        /// <param name="data">A data array of <see cref="Envelope"/>'s</param>
        /// <returns>Success/Error http result</returns>
        [HttpPut, HttpPost]
        public IHttpActionResult Put([FromBody]Envelope[] data)
        {
            
            if (data != null && ModelState.IsValid)
            {

                try
                {
                    foreach (Envelope d in data)
                        Store.Put(d);
                }
                catch(Exception error)
                {
                    return InternalServerError(error);
                }
                
                return Ok();

            }

            return InternalServerError();

        }

        /// <summary>
        /// <see cref="IStore.Delete(string)"/>
        /// /// <see cref="IStore.Delete(string, DateTime, DateTime)"/>
        /// </summary>
        /// <param name="path">The data path</param>
        /// <param name="start">The start index as a unix timestamp or null</param>
        /// <param name="end">The end index as a unix timestamp or null</param>
        /// <returns>Success/Error http result</returns>
        [HttpDelete]
        public IHttpActionResult Delete(string path, long? start, long? end)
        {

            long count = 0;

            try {

                if(start == null || end == null)
                    count = Store.Delete(path, ActionContext);
                else
                    count = Store.Delete(path, HTTP.FromUnixTimestamp(start.Value), HTTP.FromUnixTimestamp(end.Value), ActionContext);
            }
            catch(Exception error)
            {
                return InternalServerError(error);
            }

            return ResponseMessage(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(Convert.ToString(count)) });

        }
    }
}
