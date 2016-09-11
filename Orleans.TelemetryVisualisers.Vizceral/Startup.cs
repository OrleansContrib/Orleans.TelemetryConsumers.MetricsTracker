using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Routing;
using Microsoft.AspNet.SignalR;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Orleans;
using Orleans.Runtime;
using Orleans.Runtime.Configuration;
using Orleans.Streams;
using Orleans.TelemetryConsumers.MetricsTracker;
using Orleans.TelemetryVisualisation.Vizceral.Hubs;
using Orleans.TelemetryVisualisation.Vizceral.Models;
using Owin;

namespace Orleans.TelemetryVisualisation.Vizceral
{
    public class Startup
    {
        private VizceralRootObject _rootDataObject;
        private List<Node> _silos = new List<Node>();
        private IHubContext _metricsHub;
        private const string MetricsStreamProvider = "SimpleStreamProvider";
        private Dictionary<string, MetricsSnapshot> _snapshotHistoryCache;
        

        public void Configuration(IAppBuilder app)
        {
            app.MapSignalR();

            ConfigureGrainClient();

            _snapshotHistoryCache = new Dictionary<string, MetricsSnapshot>();

            SetupTelemetryStreamPump().Wait();
        }

        public void ConfigureGrainClient()
        {
            var config = ClientConfiguration.LocalhostSilo();

            config.AddSimpleMessageStreamProvider(MetricsStreamProvider);

            // Attempt to connect a few times to overcome transient failures and to give the silo enough 
            // time to start up when starting at the same time as the client (useful when deploying or during development).
            const int initializeAttemptsBeforeFailing = 5;

            int attempt = 0;
            while (true)
            {
                try
                {
                    GrainClient.Initialize(config);
                    break;
                }
                catch (SiloUnavailableException e)
                {
                    attempt++;
                    if (attempt >= initializeAttemptsBeforeFailing)
                    {
                        throw;
                    }
                    Thread.Sleep(TimeSpan.FromSeconds(2));
                }
            }
        }

        public async Task SetupTelemetryStreamPump()
        {
            var metricsStreamProvider = GrainClient.GetStreamProvider(MetricsStreamProvider);
            var clusterStream = metricsStreamProvider.GetStream<MetricsSnapshot>(Guid.Empty, "ClusterMetricSnapshots");
            var siloStream = metricsStreamProvider.GetStream<MetricsSnapshot>(Guid.Empty, "SiloMetricSnapshots");

            var metricsGrain = GrainClient.GrainFactory.GetGrain<IClusterMetricsGrain>(Guid.Empty);
            //config.StreamingProviderName = "SystemMetricsStream";
            await metricsGrain.Configure(new MetricsConfiguration
            {
                Enabled = true, // default
                SamplingInterval = TimeSpan.FromSeconds(1), // default
                ConfigurationInterval = TimeSpan.FromSeconds(10), // default
                StaleSiloMetricsDuration = TimeSpan.FromSeconds(30), // default
                TrackExceptionCounters = true,
                TrackMethodGrainCalls = true,
                StreamingProviderName = MetricsStreamProvider,
                HistoryLength = 30 // default
            });

            _rootDataObject = new VizceralRootObject
            {
                renderer = "global",
                name = "edge",
                serverUpdateTime = DateTime.UtcNow.Ticks,
                maxVolume = 100000,
                nodes = new List<Node>
                {
                    new Node
                    {
                        renderer ="region",
                        name = "INTERNET",
                        updated = DateTime.UtcNow.Ticks,
                        @class = "normal",
                        nodes = new List<Node>(),
                    },
                },
                connections = new List<Connection>()

            };

            _metricsHub = GlobalHost.ConnectionManager.GetHubContext<VizceralHub>();

            await clusterStream.SubscribeAsync((data, token) =>
            {
                PushClusterMetricsToConsumers();
                return TaskDone.Done;
            });

            await siloStream.SubscribeAsync((data, token) =>
            {
                data.Source = System.Uri.EscapeDataString(data.Source);

                AddReplaceSiloStatsToMetrics(data, _snapshotHistoryCache.ContainsKey(data.Source) ? _snapshotHistoryCache[data.Source] : null);                
                PushSiloMetricsToConsumers();
                return TaskDone.Done;
            });            
        }

        private Tuple<Node, List<Connection>> CreateSiloRecords(MetricsSnapshot snapshot, MetricsSnapshot previousSnapshot, List<Node> silos)
        {
            var grainCalls = ExtractGrainSpecificMetrics(snapshot).ToList();            

            var node = new Node
            {
                renderer = "region",
                name = snapshot.Source,
                updated = DateTime.UtcNow.Ticks,
                @class = "normal",
                maxVolume = 10000,
                nodes = grainCalls,
                connections = grainCalls.Select(g => new Connection()
                {
                    source="INTERNET",
                    target = g.name,
                    metrics = GetMetricsForGrainType(snapshot, previousSnapshot, g.name),
                    @class = "normal",
                    notices =new List<Notice>()
                }).ToList(),              
            };
            var connections =
                silos.Select(
                    s =>
                        new Connection()
                        {
                            source = snapshot.Source,
                            target = s.name,
                            metrics = GetInterSiloMetrics(snapshot, previousSnapshot, snapshot.Source, s.name),
                            @class = "normal"
                        }).ToList();

            connections.Add(new Connection
            {
                source = "INTERNET",
                target = snapshot.Source,
                metrics = GetClientGrainCalls(snapshot, previousSnapshot),
                @class = "normal",
            });

            return new Tuple<Node, List<Connection>>(node, connections);
        }

        private Metrics GetClientGrainCalls(MetricsSnapshot snapshot, MetricsSnapshot previousSnapshot)
        {
            return new Metrics
            {
                normal = AggregatedMetricValue(snapshot, previousSnapshot, "GrainMethodCall"),
                danger = AggregatedMetricValue(snapshot, previousSnapshot, "GrainException"),
                warning = AggregatedMetricValue(snapshot, previousSnapshot, "GrainInvokeTimeout"),
            };
        }

        private Metrics GetInterSiloMetrics(MetricsSnapshot snapshot, MetricsSnapshot previousSnapshot, string sourceSilo, string destinationSilo)
        {
            return new Metrics() {normal = 100, danger = 1, warning = 1};
        }

        private Metrics GetMetricsForGrainType(MetricsSnapshot snapshot, MetricsSnapshot previousSnapshot, string grainTypeName)
        {
            return new Metrics()
            {
                normal =
                    AggregatedMetricValue(snapshot, previousSnapshot,
                        $"GrainMethodCall:{grainTypeName}"),
                danger = AggregatedMetricValue(snapshot, previousSnapshot, $"GrainException:{grainTypeName}" ),
                warning = AggregatedMetricValue(snapshot, previousSnapshot, $"GrainInvokeTimeout:{grainTypeName}"),
            };

        }

        private void AddReplaceSiloStatsToMetrics(MetricsSnapshot snapshot, MetricsSnapshot previousSnapshot)
        {
            var siloRecord = CreateSiloRecords(snapshot, previousSnapshot, _silos);

                siloRecord.Item1.nodes.Add(new Node
                {
                    name = "INTERNET",
                });
                if (_rootDataObject.nodes.Exists(n => n.name == snapshot.Source))
                {
                    _rootDataObject.nodes.Remove(_rootDataObject.nodes.Single(n => n.name == snapshot.Source));
                }
                _rootDataObject.nodes.Add(siloRecord.Item1);

            _rootDataObject.connections.Where(c => c.source == snapshot.Source || (c.target == snapshot.Source && c.source == "INTERNET")).ToList().ForEach(c => _rootDataObject.connections.Remove(c));
            _rootDataObject.connections.AddRange(siloRecord.Item2);


            if (_silos.Any(s => s.name != siloRecord.Item1.name))
            {
                _silos.Add(siloRecord.Item1);
                _snapshotHistoryCache.Add(snapshot.Source, snapshot);
            }
            
            _snapshotHistoryCache[snapshot.Source] = snapshot;            
        }

        private IEnumerable<Node> ExtractGrainSpecificMetrics(MetricsSnapshot snapshot)
        {
            var grainTypesInSnapshot =
                snapshot.Metrics.Keys.Where(k => k.StartsWith("GrainMethodCall")).GroupBy(k => k.Split(':')[1], k => k);

            foreach (var grainType in grainTypesInSnapshot)
            {
                yield return new Node
                {
                    name = grainType.Key,
                    metadata = new Metadata(),
                    @class = "normal",
                    updated = DateTime.UtcNow.Ticks,
                };
            }
        }

        private double AggregatedMetricValue(MetricsSnapshot snapshot, MetricsSnapshot previousSnapshot, string metricPrefix, string[] excluding = null)
        {
            if (previousSnapshot == null)
            {
                return snapshot.Metrics.Where(m => m.Key.StartsWith(metricPrefix)).Sum(m => m.Value);
            }
            // We should really be caching the resultant summing from the previous snapshots, some low hanging fruit for the future.
            if (excluding == null)
            {
                return snapshot.Metrics.Where(m => m.Key.StartsWith(metricPrefix)).Sum(m => m.Value) - previousSnapshot.Metrics.Where(m => m.Key.StartsWith(metricPrefix)).Sum(m => m.Value);
            }
            return
                snapshot.Metrics.Where(m => m.Key.StartsWith(metricPrefix) && !excluding.Contains(m.Key))
                    .Sum(m => m.Value) - previousSnapshot.Metrics.Where(m => m.Key.StartsWith(metricPrefix) && !excluding.Contains(m.Key))
                    .Sum(m => m.Value);
        }
        private void PushSiloMetricsToConsumers()
        {
                _metricsHub.Clients.All.updateTelemetry(_rootDataObject);            
        }

        private void PushClusterMetricsToConsumers()
        {

        }
    }
}