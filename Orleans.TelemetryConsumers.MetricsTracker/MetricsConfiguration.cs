using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orleans.TelemetryConsumers.MetricsTracker
{
    public class MetricsConfiguration
    {
        /// <summary>
        /// Whether or not the MetricsTrackerTelemetryConsumer should report silo statistics.
        /// Depending on ConfigurationInterval, there will be some delay before metrics begin
        /// reporting or stop reporting.
        /// </summary>
        public bool Enabled = true;

        /// <summary>
        /// The time between metrics samples being taken and pushed to ClusterMetricsGrain.
        /// </summary>
        public TimeSpan SamplingInterval = TimeSpan.FromSeconds(1);

        /// <summary>
        /// The time between polling calls to check for metrics configuration changes
        /// until we can figure out a way to call into the MetricsTrackerTelemetryConsumer
        /// from within ClusterMetricsGrain.
        /// </summary>
        public TimeSpan ConfigurationInterval = TimeSpan.FromSeconds(10);

        /// <summary>
        /// The duration after which silo metrics data are considered stale.
        /// There may be problems or delays in communicating with the affected silo(s).
        /// </summary>
        public TimeSpan StaleSiloMetricsDuration = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Track total exceptions reported, total number of unique exceptions reported,
        /// and a counter for each Exception type name.
        /// </summary>
        public bool TrackExceptionCounters = false;

        /// <summary>
        /// Track all grain method calls, creating an invocation counter for each method.
        /// </summary>
        public bool TrackMethodGrainCalls = false;

        /// <summary>
        /// Returns true if either TrackExceptionCounters or TrackMethodGrainCalls are true.
        /// </summary>
        public bool NeedSiloInterceptor => TrackExceptionCounters || TrackMethodGrainCalls;

        /// <summary>
        /// The number of historical samples to keep for all counters and metrics.
        /// Default is 30.
        /// </summary>
        public int HistoryLength = 30;

        /// <summary>
        /// The name of the streaming provider to use for publishing metrics snapshots.
        /// </summary>
        public string StreamingProviderName = null;
    }
}
