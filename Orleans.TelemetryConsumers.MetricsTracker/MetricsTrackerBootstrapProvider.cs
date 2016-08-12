using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Orleans;
using Orleans.Runtime;
using Orleans.Providers;

namespace Orleans.TelemetryConsumers.MetricsTracker
{
    public class MetricsTrackerBootstrapProvider : IBootstrapProvider
    {
        public string Name { get { return "MetricsTracker"; } }

        IProviderRuntime Runtime;
        IProviderConfiguration Config;

        Logger logger;

        public Task Init(string name, IProviderRuntime providerRuntime, IProviderConfiguration config)
        {
            Runtime = providerRuntime;
            Config = config;

            logger = Runtime.GetLogger(nameof(MetricsTracker));

            var telemetryConsumer = new MetricsTrackerTelemetryConsumer(providerRuntime);
            LogManager.TelemetryConsumers.Add(telemetryConsumer);

            return TaskDone.Done;
        }

        public Task Close()
        {
            return TaskDone.Done;
        }
    }
}
