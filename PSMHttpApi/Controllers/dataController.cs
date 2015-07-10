using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Collections.Generic;
using System;
using PSMonitor.Stores;

namespace PSMonitor.Controllers
{

    public class dataController : ApiController
    {
        [HttpGet]
        public IHttpActionResult Get()
        {
            return NotFound();
        }

        [HttpGet]
        public IHttpActionResult Get(string path)
        {

            Entry entry;

            try {

                entry = PSM.Store.Get(path);

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
                                PSM.Store.Get(
                                    path,
                                    start,
                                    end
                            )));

                        break;

                    case "time":

                        response.Content = new StreamContent(
                            new EntryJSONStream(
                                PSM.Store.Get(
                                    path,
                                    HTTP.FromUnixTimestamp(start / 1000),
                                    HTTP.FromUnixTimestamp(end / 1000)
                            )));

                        break;

                    default:
                        throw new NullReferenceException();
                }
               
                response.Content.Headers.Add("Content-Type", "application/json");

            }
            catch(Exception error)
            {
                return InternalServerError(error);
            }

            return ResponseMessage(response);

        }

        [HttpPut, HttpPost]
        public IHttpActionResult Put([FromBody]Envelope[] data)
        {
            
            if (data != null && ModelState.IsValid)
            {

                try
                {
                    foreach (Envelope d in data)
                        WebApiApplication.master.Inject(d);
                }
                catch(Exception error)
                {
                    return InternalServerError(error);
                }
                
                return Ok();

            }

            return InternalServerError();

        }

        [HttpDelete]
        public IHttpActionResult Delete(string path, long? start, long? end)
        {

            long count = 0;

            try {

                if(start == null || end == null)
                    count = PSM.Store.Delete(path);
                else
                    count = PSM.Store.Delete(path, HTTP.FromUnixTimestamp(start.Value), HTTP.FromUnixTimestamp(end.Value));
            }
            catch(Exception error)
            {
                return InternalServerError(error);
            }

            return ResponseMessage(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(Convert.ToString(count)) });

        }
    }
}
