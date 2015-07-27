using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNet.SignalR;

namespace PSMonitor
{
    
    public class DataReceivedHub : Hub
    {
        public override Task OnConnected()
        {
            return base.OnConnected();
        }
    }
}