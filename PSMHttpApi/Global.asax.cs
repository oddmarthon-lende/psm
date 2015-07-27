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

namespace PSMonitor
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        
        internal class Helper : PSM
        {
            private IHubContext hub = GlobalHost.ConnectionManager.GetHubContext<DataReceivedHub>();

            public void Inject(Envelope data)
            {
                hub.Clients.All.OnData(data);
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
