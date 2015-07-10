using System.Web.Http;
using System.Web.Mvc;
using System.Web.Routing;
using System;
using System.Runtime.Serialization.Json;
using System.IO;
using System.Collections.Generic;

namespace PSMonitor
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        

        internal class Helper : PSM
        {
            public void Inject(Envelope data)
            {
                base.OnData(data);
            }

        }

        internal static Helper master = new Helper();

        protected void Application_Start()
        {
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
        }
    }
}
