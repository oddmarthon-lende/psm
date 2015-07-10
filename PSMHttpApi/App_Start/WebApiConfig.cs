using System.Web.Http;

namespace PSMonitor
{
    public static class WebApiConfig
    {
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

        }
    }
}
