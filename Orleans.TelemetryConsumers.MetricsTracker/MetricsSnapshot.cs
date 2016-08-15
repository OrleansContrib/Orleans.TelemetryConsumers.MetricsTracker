using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orleans.TelemetryConsumers.MetricsTracker
{
    /// <summary>
    /// Records the current values for all of the counters and metrics being tracked,
    /// as well as origin (Source), time-of-creation (SnapShotTime) data, and the number of
    /// Silos represented by the snapshot.
    /// </summary>
    public class MetricsSnapshot
    {
        /// <summary>
        /// The date and time the snapshot was created. Used to determine when the data is stale
        /// or to detect silo failures and network partitions.
        /// </summary>
        public DateTime SnapshotTime;

        /// <summary>
        /// Where the snapshot was created: either named after the ClusterMetricsGrain type 
        /// or a unique Silo identity.
        /// </summary>
        public string Source;

        /// <summary>
        /// The number of silos represented in the snapshot. For silo snapshots: 1.
        /// For cluster aggregate snapshots, it depends on the number of silos.
        /// </summary>
        public int SiloCount = 1;

        /// <summary>
        /// Counters for tracking events, exceptions, and grain method calls. Not populated yet.
        /// </summary>
        public Dictionary<string, long> Counters;

        /// <summary>
        /// Floating-point metrics tracking. Currently storing all the counters.
        /// </summary>
        public Dictionary<string, double> Metrics;

        /// <summary>
        /// Waiting for IncrementTimeSpanMetric or similar to hook this up.
        /// </summary>
        public Dictionary<string, TimeSpan> TimeSpanMetrics;

        public MetricsSnapshot()
        {
            SnapshotTime = DateTime.UtcNow;
            Source = "Internal";

            Counters = new Dictionary<string, long>();
            Metrics = new Dictionary<string, double>();
            TimeSpanMetrics = new Dictionary<string, TimeSpan>();
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.Append($"MetricsSnapshot from {Source} (SiloCount={SiloCount})\n");

            foreach (var metric in Metrics)
                sb.Append($"--- [Metric] {metric.Key}={metric.Value}\n");

            return sb.ToString();
        }
    }
}
