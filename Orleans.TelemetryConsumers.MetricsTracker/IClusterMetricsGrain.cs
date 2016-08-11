using System;
using System.Threading.Tasks;

namespace Orleans.TelemetryConsumers.MetricsTracker
{
    public interface IClusterMetricsGrain : IGrainWithGuidKey
    {
        Task ReportSiloStatistics(MetricsSnapshot snapshot);

        Task<MetricsSnapshot> GetClusterMetrics();
    }
}
