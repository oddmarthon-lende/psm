using System.Web.Http;
using PSM.Stores;

namespace PSM.HttpApi.Controllers
{

    public class informationController : ApiController
    {       

        public IHttpActionResult Get()
        {
            return Json(new HTTP.Information(PSM.Store.Get().Index));
        }

    }
}
