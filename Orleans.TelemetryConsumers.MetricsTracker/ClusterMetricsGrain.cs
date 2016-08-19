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

        MetricsConfiguration Configuration;

        MetricsSnapshot ClusterSnapshot;
        Dictionary<string, MetricsSnapshot> SiloSnapshots;

        ObserverSubscriptionManager<IClusterMetricsGrainObserver> Subscribers;

        // Streams
        IStreamProvider StreamProvider;
        IAsyncStream<MetricsSnapshot> ClusterSnapshotStream;
        IAsyncStream<MetricsSnapshot> SiloSnapshotStream;

        public override Task OnActivateAsync()
        {
            try
            {
                logger = GetLogger("ClusterMetricsGrain");

                Configuration = new MetricsConfiguration();

                ClusterSnapshot = new MetricsSnapshot { Source = nameof(ClusterMetricsGrain) };
                SiloSnapshots = new Dictionary<string, MetricsSnapshot>();

                Subscribers = new ObserverSubscriptionManager<IClusterMetricsGrainObserver>();

                return base.OnActivateAsync();
            }
            catch (Exception ex)
            {
                if (logger != null)
                    logger.TrackException(ex);

                throw;
            }
        }

        public async Task Configure(MetricsConfiguration config)
        {
            try
            {
                Configuration = config;

                // TODO: figure out how to get MetricsTrackerTelemetryConsumer to get notifications
                
                // using grain observables
                if (Subscribers != null)
                    Subscribers.Notify(o => o.Configure(config));

                // using streams
                if (!string.IsNullOrWhiteSpace(Configuration.StreamingProviderName))
                {
                    try
                    {
                        StreamProvider = GrainClient.GetStreamProvider(Configuration.StreamingProviderName);

                        ClusterSnapshotStream = StreamProvider.GetStream<MetricsSnapshot>(Guid.Empty, "ClusterMetricSnapshots");
                        SiloSnapshotStream = StreamProvider.GetStream<MetricsSnapshot>(Guid.Empty, "SiloMetricSnapshots");
                    }
                    catch (Exception ex)
                    {
                        // probably here because stream provider wasn't found
                        // TODO: handle better
                        logger.TrackException(ex);
                        // don't rethrow the exception
                    }
                }
                else
                {
                    if (ClusterSnapshotStream != null)
                        await ClusterSnapshotStream.OnCompletedAsync();

                    if (SiloSnapshotStream != null)
                        await SiloSnapshotStream.OnCompletedAsync();

                    ClusterSnapshotStream = null;
                    SiloSnapshotStream = null;
                }

                logger.IncrementMetric("ClusterMetricsConfigured");
            }
            catch (Exception ex)
            {
                logger.TrackException(ex);
                throw;
            }
        }

        public async Task Start()
        {
            Configuration.Enabled = true;

            await Configure(Configuration);
        }
        
        // it's not expected that anyone would want to call Stop, but it seems odd to leave it out
        public async Task Stop()
        {
            Configuration.Enabled = false;

            await Configure(Configuration);
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

        public async Task ReportSiloStatistics(MetricsSnapshot snapshot)
        {
            try
            {
                // add or replace the snapshot for the silo this came from
                if (!SiloSnapshots.ContainsKey(snapshot.Source))
                    SiloSnapshots.Add(snapshot.Source, snapshot);
                else
                    SiloSnapshots[snapshot.Source] = snapshot;
                
                if (SiloSnapshotStream != null)
                    await SiloSnapshotStream.OnNextAsync(snapshot);

                // recalculate cluster metrics snapshot
                // any time a silo snapshot is added or updated
                ClusterSnapshot = CalculateClusterMetrics(SiloSnapshots.Values.ToList());

                if (ClusterSnapshotStream != null)
                    await ClusterSnapshotStream.OnNextAsync(ClusterSnapshot);

                logger.IncrementMetric("SiloMetricsReported");
            }
            catch (Exception ex)
            {
                logger.TrackException(ex);
                throw;
            }
        }

        // TODO: add unit tests
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

        public Task<List<MetricsSnapshot>> GetAllMetrics()
        {
            try
            {
                // create a list of MetricsSnapshots with the cluster aggregate snapshot first
                
                var allMetrics = SiloSnapshots.Values.ToList();

                if (allMetrics.Count == 0)
                    allMetrics.Add(ClusterSnapshot);
                else
                    allMetrics.Insert(0, ClusterSnapshot);

                return Task.FromResult(allMetrics);
            }
            catch (Exception ex)
            {
                logger.TrackException(ex);
                throw;
            }
        }

        public Task<MetricsConfiguration> GetMetricsConfiguration()
        {
            return Task.FromResult(Configuration);
        }

        string GetStreamName(MetricType type, string metric)
        {
            var typeName =
                type == MetricType.Counter ? "Counter"
                : type == MetricType.Metric ? "Metric"
                : type == MetricType.TimeSpanMetric ? "TimeSpanMetric"
                : null;

            if (typeName == null)
                throw new ArgumentException("Unknown MetricType");

            return $"{typeName}-{metric}";
        }
    }
}
