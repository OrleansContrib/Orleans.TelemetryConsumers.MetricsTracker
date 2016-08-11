using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orleans.TelemetryConsumers.MetricsTracker
{
    public class MetricsSnapshot
    {
        public DateTime SnapshotTime;
        public string Source;
        public int SiloCount = 1;

        public Dictionary<string, long> Counters;
        public Dictionary<string, double> Metrics;
        public Dictionary<string, TimeSpan> TimeSpanMetrics;

        public MetricsSnapshot()
        {
            SnapshotTime = DateTime.UtcNow;
            Source = "Internal";

            Counters = new Dictionary<string, long>();
            Metrics = new Dictionary<string, double>();
            TimeSpanMetrics = new Dictionary<string, TimeSpan>();
        }
    }
}
