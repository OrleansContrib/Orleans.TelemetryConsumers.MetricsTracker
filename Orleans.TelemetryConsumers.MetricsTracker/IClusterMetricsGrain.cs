using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Orleans.TelemetryConsumers.MetricsTracker
{
    public interface IClusterMetricsGrain : IGrainWithGuidKey
    {
        /// <summary>
        /// Configure the metrics gathering behavior of the MetricsTrackerTelemetryConsumer
        /// and the cooperating ClusterMetricsGrain.
        /// </summary>
        /// <param name="config"></param>
        Task Configure(MetricsConfiguration config);

        /// <summary>
        /// Updates the current configuration's Enabled to true, which will trigger
        /// the MetricsTrackerTelemetryConsumers to start reporting silo metrics to
        /// ClusterMetricsGrain.
        /// </summary>
        Task Start();

        /// <summary>
        /// Updates the current configuration's Enabled to false, which will trigger
        /// the MetricsTrackerTelemetryConsumers to stop reporting silo metrics to
        /// ClusterMetricsGrain.
        /// </summary>
        Task Stop();

        /// <summary>
        /// Get a snapshot of the current values for all metrics and counters;
        /// </summary>
        Task<MetricsSnapshot> GetClusterMetrics();

        /// <summary>
        /// Get a metrics snapshot for the cluster as a whole and for each of the silos.
        /// </summary>
        Task<List<MetricsSnapshot>> GetAllMetrics();

        /// <summary>
        /// Pull configuration data.
        /// </summary>
        Task<MetricsConfiguration> GetMetricsConfiguration();

        /// <summary>
        /// Called by MetricsTrackerTelemetryConsumer to report silo statistics to
        /// ClusterMetricsGrain, where they're stored separately as well as in aggregate.
        /// </summary>
        /// <param name="snapshot"></param>
        Task ReportSiloStatistics(MetricsSnapshot snapshot);

        /// <summary>
        /// Trying to get this to work. It doesn't yet. There's a work-around in place.
        /// This is not the grain method you were looking for.
        /// </summary>
        /// <param name="observer"></param>
        /// <returns></returns>
        Task Subscribe(IClusterMetricsGrainObserver observer);
    }
}
