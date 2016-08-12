using System;
using System.Linq;
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

        ObserverSubscriptionManager<IClusterMetricsGrainObserver> Subscribers;

        MetricsSnapshot ClusterSnapshot;
        Dictionary<string, MetricsSnapshot> SiloSnapshots;

        #region Streams

        // TODO: use these streams

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

                Subscribers = new ObserverSubscriptionManager<IClusterMetricsGrainObserver>();

                SiloSnapshots = new Dictionary<string, MetricsSnapshot>();
                ClusterSnapshot = new MetricsSnapshot { Source = nameof(ClusterMetricsGrain) };

                return base.OnActivateAsync();
            }
            catch (Exception ex)
            {
                if (logger != null)
                    logger.TrackException(ex);

                throw;
            }
        }

        public Task Start()
        {
            Subscribers.Notify(o => o.EnableClusterMetrics());
            return TaskDone.Done;
        }

        // it's not expected that anyone would want to call Stop, but it seems odd to leave it out
        public Task Stop()
        {
            Subscribers.Notify(o => o.DisableClusterMetrics());
            return TaskDone.Done;
        }

        // used by a custom MetricsTrackerTelemetryConsumer on each silo
        // to make themselves aware of when they should or shouldn't push their metrics here
        public Task Subscribe(IClusterMetricsGrainObserver observer)
        {
            try
            {
                // don't subscribe twice
                if (!Subscribers.IsSubscribed(observer))
                    Subscribers.Subscribe(observer);

                return TaskDone.Done;
            }
            catch (Exception ex)
            {
                logger.TrackException(ex);
                throw;
            }
        }

        void LogMetricsSnapshot(MetricsSnapshot snapshot)
        {
            try
            {
                logger.TrackTrace($"MetricsSnapshot from {snapshot.Source} (SiloCount = {snapshot.SiloCount})");

                foreach (var counter in snapshot.Counters)
                    logger.TrackTrace($"[Counter] {counter.Key} = {counter.Value}");

                foreach (var metric in snapshot.Metrics)
                    logger.TrackTrace($"[Metric] {metric.Key} = {metric.Value}");

                foreach (var metric in snapshot.TimeSpanMetrics)
                    logger.TrackTrace($"[TimeSpan Metric] {metric.Key} = {metric.Value}");
            }
            catch (Exception ex)
            {
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

                // recalculate cluster metrics snapshot
                // any time a silo snapshot is added or updated
                ClusterSnapshot = CalculateClusterMetrics(SiloSnapshots.Values.ToList());



                // for debugging purposes
                logger.TrackTrace("---NEW SILO METRICS SNAPSHOT---");
                LogMetricsSnapshot(snapshot);
                logger.TrackTrace("---NEW CLUSTER METRICS SNAPSHOT---");
                LogMetricsSnapshot(ClusterSnapshot);



                logger.IncrementMetric("SiloStatisticsReported");
                logger.TrackTrace("SiloStatisticsReported");



                // uncomment the following code to see how exceptions are counted:
                //   total exceptions reported
                //   unique exceptions reported
                //   specific counts per exception type

                // TODO: remove this!
                // generate exceptions about 10% of the time
                //var rand = new Random(DateTime.UtcNow.Millisecond);
                //if (rand.NextDouble() < 0.1)
                //    throw new ApplicationException("RandomException");



                return TaskDone.Done;
            }
            catch (Exception ex)
            {
                logger.TrackException(ex);
                throw;
            }
        }

        MetricsSnapshot CalculateClusterMetrics(IList<MetricsSnapshot> siloSnapshots)
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

                result.SiloCount = siloSnapshots.Count;

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
