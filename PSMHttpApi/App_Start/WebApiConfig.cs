using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Controllers;

namespace PSMonitor
{
    public static class WebApiConfig
    {
        private class MessageHandler : DelegatingHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return base.SendAsync(request, cancellationToken);
            }
        }

        public static void Register(HttpConfiguration config)
        {
            // Web API routes
            config.MapHttpAttributeRoutes();
            
            config.Routes.MapHttpRoute("DataApi", "{controller}/{path}/{start}/{end}/{type}", new {
                path = RouteParameter.Optional,
                start = RouteParameter.Optional,
                end = RouteParameter.Optional,
                type = RouteParameter.Optional
            });

            config.Routes.MapHttpRoute("KeysApi", "{controller}/{path}", new
            {
                path = RouteParameter.Optional
            });

            config.Routes.MapHttpRoute("InfoApi", "{controller}");

            config.MessageHandlers.Add(new MessageHandler());

        }
    }
}
