using Microsoft.AspNet.SignalR;
using Orleans.TelemetryVisualisation.Vizceral.Models;

namespace Orleans.TelemetryVisualisation.Vizceral.Hubs
{
    public class VizceralHub : Hub
    {
        public void SendLatestSystemTelemetry(VizceralRootObject root)
        {
            Clients.All.updateTelemetry(root);
        }
    }
}