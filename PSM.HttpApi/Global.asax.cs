using System.Web.Http;
using System.Web.Mvc;
using System.Web.Routing;
using System.Collections.Generic;
using System.Web.WebSockets;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.IO;
using System.Timers;
using System;
using Microsoft.AspNet.SignalR;
using PSM.Stores;
using System.Net.Http;
using System.Web.Http.Controllers;
using System.Linq;

namespace PSM.HttpApi
{   

    public class WebApiApplication : System.Web.HttpApplication
    {
                
        protected void Application_Start()
        {
            
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
                        
        }


    }
}
