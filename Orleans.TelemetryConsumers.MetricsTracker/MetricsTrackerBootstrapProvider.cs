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

            // TODO: harden this for production use
            // TODO: add tracking of the time each grain method call took to complete
            // TODO: make this optional, enabled or disabled via ClusterMetricsGrain calls
            Runtime.SetInvokeInterceptor(async (method, request, grain, invoker) =>
            {
                try
                {
                    // Invoke the request and return the result back to the caller.
                    var result = await invoker.Invoke(grain, request);

                    //if (logger.IsVerbose)
                        logger.IncrementMetric($"GrainMethodCall:{grain.GetType().Name}.{method.Name}");

                    return result;
                }
                catch (Exception ex)
                {
                    // log all uncaught exceptions from grain method invocations
                    logger.TrackException(ex);
                    throw;
                }
            });

            return TaskDone.Done;
        }

        public Task Close()
        {
            return TaskDone.Done;
        }
    }
}
