using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Orleans;
using Orleans.Streams;
using Orleans.Runtime;

namespace Orleans.TelemetryConsumers.MetricsTracker
{
    public class ClusterMetricsGrain : Grain, IClusterMetricsGrain
    {
        Logger logger;

        MetricsSnapshot ClusterSnapshot;
        Dictionary<string, MetricsSnapshot> SiloSnapshots;

        #region Streams

        // TODO: use these

        IAsyncStream<MetricsSnapshot> SnapshotStream;

        Dictionary<string, IAsyncStream<long>> CounterStreams;
        Dictionary<string, IAsyncStream<double>> MetricStreams;
        Dictionary<string, IAsyncStream<TimeSpan>> TimeSpanMetricStreams;
        Dictionary<string, IAsyncStream<MeasuredRequest>> RequestStreams;

        #endregion

        public override Task OnActivateAsync()
        {
            try
            {
                logger = GetLogger("ClusterMetricsGrain");

                SiloSnapshots = new Dictionary<string, MetricsSnapshot>();
                ClusterSnapshot = CalculateClusterMetrics(SiloSnapshots.Values);

                logger.IncrementMetric("SampleMetric");

                return base.OnActivateAsync();
            }
            catch (Exception ex)
            {
                if (logger != null)
                    logger.TrackException(ex);

                throw;
            }
        }

        public Task ReportSiloStatistics(MetricsSnapshot snapshot)
        {
            try
            {
                // add or replace the snapshot for the silo this came from
                if (!SiloSnapshots.ContainsKey(snapshot.Source))
                    SiloSnapshots.Add(snapshot.Source, snapshot);
                else
                    SiloSnapshots[snapshot.Source] = snapshot;

                logger.IncrementMetric("SiloStatisticsReported");
                logger.TrackEvent("SiloStatisticsReported");
                logger.Verbose("SiloStatisticsReported");
                logger.Info("SiloStatisticsReported");
                logger.TrackTrace("SiloStatisticsReported");

                return TaskDone.Done;
            }
            catch (Exception ex)
            {
                logger.TrackException(ex);
                throw;
            }
        }

        MetricsSnapshot CalculateClusterMetrics(IEnumerable<MetricsSnapshot> siloSnapshots)
        {
            try
            {
                var result = new MetricsSnapshot { Source = nameof(ClusterMetricsGrain) };

                foreach (var siloSnapshot in siloSnapshots)
                {
                    foreach (var siloCounter in siloSnapshot.Counters)
                    {
                        if (!result.Counters.ContainsKey(siloCounter.Key))
                            result.Counters.Add(siloCounter.Key, siloCounter.Value);
                        else
                            result.Counters[siloCounter.Key] += siloCounter.Value;
                    }

                    foreach (var siloMetric in siloSnapshot.Metrics)
                    {
                        if (!result.Metrics.ContainsKey(siloMetric.Key))
                            result.Metrics.Add(siloMetric.Key, siloMetric.Value);
                        else
                            result.Metrics[siloMetric.Key] += siloMetric.Value;
                    }

                    foreach (var siloMetric in siloSnapshot.TimeSpanMetrics)
                    {
                        if (!result.TimeSpanMetrics.ContainsKey(siloMetric.Key))
                            result.TimeSpanMetrics.Add(siloMetric.Key, siloMetric.Value);
                        else
                            result.TimeSpanMetrics[siloMetric.Key] += siloMetric.Value;
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                logger.TrackException(ex);
                throw;
            }
        }

        public Task<MetricsSnapshot> GetClusterMetrics()
        {
            return Task.FromResult(ClusterSnapshot);
        }

        //string GetStreamName(MetricType type, string metric)
        //{
        //    var typeName =
        //        type == MetricType.Counter ? "Counter"
        //        : type == MetricType.Metric ? "Metric"
        //        : type == MetricType.TimeSpanMetric ? "TimeSpanMetric"
        //        : null;

        //    if (typeName == null)
        //        throw new ArgumentException("Unknown MetricType");

        //    return $"{typeName}-{metric}";
        //}
    }
}
