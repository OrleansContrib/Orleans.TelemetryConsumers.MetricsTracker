using System;
using System.Threading.Tasks;

namespace Orleans.TelemetryConsumers.MetricsTracker
{
    public interface IClusterMetricsGrain : IGrainWithGuidKey
    {
        Task Subscribe(IClusterMetricsGrainObserver observer);

        Task Start();

        Task Stop();

        Task ReportSiloStatistics(MetricsSnapshot snapshot);

        Task<MetricsSnapshot> GetClusterMetrics();
    }
}
