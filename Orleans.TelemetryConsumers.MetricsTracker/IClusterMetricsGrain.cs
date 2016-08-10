using System;
using System.Threading.Tasks;

namespace Orleans.TelemetryConsumers.MetricsTracker
{
    internal interface IClusterMetricsGrain : IGrainWithGuidKey
    {
        Task ReportSiloStatistics(MetricsSnapshot snapshot);

        Task<MetricsSnapshot> GetClusterMetrics();
    }
}
