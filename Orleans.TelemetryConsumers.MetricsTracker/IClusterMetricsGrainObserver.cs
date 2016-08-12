using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orleans.TelemetryConsumers.MetricsTracker
{
    public interface IClusterMetricsGrainObserver : IGrainObserver
    {
        void EnableClusterMetrics(MetricsConfiguration config);
        void DisableClusterMetrics();
    }
}
