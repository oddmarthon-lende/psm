using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;

namespace PSM.HttpApi
{

    public class RealTimeDataHub : Hub
    {
        
        public override Task OnDisconnected(bool stopCalled)
        {
            Store.Unregister(Context.ConnectionId);
            return base.OnDisconnected(stopCalled);
        }

        public override Task OnConnected()
        {
            return base.OnConnected();
        }
    }
}