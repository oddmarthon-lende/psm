using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using PSMonitor.Stores;

namespace PSMonitor.Controllers
{
    public class keysController : ApiController
    {
        
        private IEnumerable<Key> GetKeys(string path)
        {
            return PSM.Store.GetKeys(path ?? "");
        }

        [HttpGet]
        public IEnumerable<Key> Get()
        {
            return GetKeys(null);
        }

        [HttpGet]
        public IEnumerable<Key> Get(string path)
        {
            return GetKeys(path);
        }
        
    }
}
