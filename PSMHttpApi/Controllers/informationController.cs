using System.Web.Http;
using PSMonitor.Stores;

namespace PSMonitor.Controllers
{

    public class informationController : ApiController
    {       

        public IHttpActionResult Get()
        {
            return Json(new HTTP.Information(PSM.Store().Index));
        }

    }
}
