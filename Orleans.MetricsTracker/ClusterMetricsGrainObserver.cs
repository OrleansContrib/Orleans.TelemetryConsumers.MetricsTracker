using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orleans.TelemetryConsumers.MetricsTracker
{
    public class ClusterMetricsGrainObserver : IClusterMetricsGrainObserver
    {
        public void DisableClusterMetrics()
        {
        }

        public void Configure(MetricsConfiguration config)
        {
        }
    }
}
