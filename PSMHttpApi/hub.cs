using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;

namespace PSMonitor
{

    public class RealTimeDataHub : Hub
    {
        
        public override Task OnDisconnected(bool stopCalled)
        {
            Controllers.Store.Unregister(Context.ConnectionId);
            return base.OnDisconnected(stopCalled);
        }

        public override Task OnConnected()
        {
            return base.OnConnected();
        }
    }
}