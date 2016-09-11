# Orleans.TelemetryConsumers.MetricsTracker
The MetricsTracker telemetry consumer for Orleans provides a high-speed, thread-safe metrics repository that consumes normal Orleans logging method calls from within grains and Orleans clients, tracks counter increment, decrement, and metric updates, and makes that data available on request to a metrics grain or by subscribing to a metrics virtual stream.

>The purpose of MetricsTracker isn't to visualize data directly, but to provide a simple, stable, performant, and effective streaming data source to feed a growing collection of useful Orleans monitoring, diagnostics & visualization tools.

Because a metrics repository is stored in each silo, there are multiple silo metrics repositories in multi-silo clusters. To get a better picture of what all those metrics look like across the whole cluster, MetricsTracker aggregates the SiloMetricsSnapshots it creates into a ClusterMetricsSnapshot within the ClusterMetricsGrain, which is the front-end facade or API for the MetricsTracker extension. Once you plug in the bootstrap provider for this extension, it's through the ClusterMetricsGrain that you'll configure the metrics sampling and virtual streaming features.

MetricsTracker was carefully designed to minimize impact, holding thread locks only where needed and for the minimum time possible, using Concurrent collection types, and sampling metrics at configurable intervals. In demo simulations, hundreds of thousands of stateless grains incrementing event counters and logging grain method calls had little apparent effect, at least without more rigorous performance measurements.

MetricsTracker can be easily added to your silo host using this Nuget package:
https://www.nuget.org/packages/Orleans.TelemetryConsumers.MetricsTracker/0.9.4-alpha

...and this recommended code-based configuration (in OrleansHostWrapper.Run if you're using the project template):

```csharp
siloHost.InitializeOrleansSilo();
siloHost.Config.AddSimpleMessageStreamProvider("SimpleStreamProvider");
siloHost.Config.AddMemoryStorageProvider("PubSubStore");
siloHost.Config.Globals
    .RegisterBootstrapProvider<MetricsTrackerBootstrapProvider>("MetricsTracker");
ok = siloHost.StartOrleansSilo();
```

In your Orleans client, after your call to GrainClient.Initialize, you can configure MetricsTracker like this:

```csharp
var metrics = GrainClient.GrainFactory.GetGrain<IClusterMetricsGrain>(Guid.Empty);
metrics.Configure(new MetricsConfiguration
   {
       Enabled = true, // default
       SamplingInterval = TimeSpan.FromSeconds(1), // default
       ConfigurationInterval = TimeSpan.FromSeconds(10), // default
       StaleSiloMetricsDuration = TimeSpan.FromSeconds(10), // default
       TrackExceptionCounters = true,
       TrackMethodGrainCalls = true,
       StreamingProviderName = "SimpleStreamProvider",
       HistoryLength = 30 // default
   }).Ignore();
```

You can then subscribe to the stream by calling a method like this one:

```csharp
async Task SubscribeToStream()
{
    StreamProvider = GrainClient.GetStreamProvider("SimpleStreamProvider");
    ClusterSnapshotStream = StreamProvider
        .GetStream<MetricsSnapshot>(Guid.Empty, "ClusterMetricSnapshots");

    await ClusterSnapshotStream.SubscribeAsync(OnNewMetricSnapshot);
}
```

Now you can respond to each new metrics snapshot, with metrics aggregated across all the silos:

```csharp
async Task OnNewMetricSnapshot(MetricsSnapshot snapshot, StreamSequenceToken token)
{
    // bare bones: functional, but doesn't tap into human visual processing capabilities
    Console.WriteLine(snapshot);
    
    // pump this data into some amazing visualization tools!
}
```

You can find a working sample project here:
https://github.com/danvanderboom/Orleans.TelemetryConsumers.MetricsTracker/tree/master/MetricsTracker.TestHost
