using System;
using System.Threading.Tasks;
using System.Threading;
using Orleans;
using Orleans.Streams;
using Orleans.Runtime.Configuration;
using Orleans.TelemetryConsumers.MetricsTracker;
using MetricsTracker.TestHost.TestDomain;

namespace Orleans.TelemetryConsumers.MetricsTracker.TestHost
{
    public class Program
    {
        static IStreamProvider StreamProvider;

        static IAsyncStream<MetricsSnapshot> ClusterSnapshotStream;
        //static IAsyncStream<MetricsSnapshot> SiloSnapshotStream;

        static void Main(string[] args)
        {
            //var SyncContext = new SynchronizationContext();
            //SynchronizationContext.SetSynchronizationContext(SyncContext);

            // The Orleans silo environment is initialized in its own app domain in order to more
            // closely emulate the distributed situation, when the client and the server cannot
            // pass data via shared memory.
            AppDomain hostDomain = AppDomain.CreateDomain("OrleansHost", null, new AppDomainSetup
            {
                AppDomainInitializer = InitSilo,
                AppDomainInitializerArguments = args,
            });

            var config = ClientConfiguration.LocalhostSilo();
            config.AddSimpleMessageStreamProvider("SimpleStreamProvider");
            //config.DefaultTraceLevel = Runtime.Severity.Verbose;
            GrainClient.Initialize(config);

            // TODO: once the previous call returns, the silo is up and running.
            //       This is the place your custom logic, for example calling client logic
            //       or initializing an HTTP front end for accepting incoming requests.

            // configure and start the reporting of silo metrics
            // UNCOMMENT BELOW to override configuration defaults
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

            // TODO: put together a better demo
            // start our silly demo simulation
            var sim = GrainClient.GrainFactory.GetGrain<ISimulatorGrain>(Guid.Empty);
            sim.StartSimulation(TimeSpan.FromMinutes(10), 200, 200, true).Ignore();

            SubscribeToStream().Wait();

            Console.WriteLine("Orleans Silo is running.\nPress Enter to terminate...");
            Console.ReadLine();

            hostDomain.DoCallBack(ShutdownSilo);
        }

        static async Task SubscribeToStream()
        {
            StreamProvider = GrainClient.GetStreamProvider("SimpleStreamProvider");
            ClusterSnapshotStream = StreamProvider
                .GetStream<MetricsSnapshot>(Guid.Empty, "ClusterMetricSnapshots");

            await ClusterSnapshotStream.SubscribeAsync(OnNewMetricSnapshot);
        }

        static async Task OnNewMetricSnapshot(MetricsSnapshot snapshot, StreamSequenceToken token)
        {
            Console.WriteLine(snapshot);
        }

        static void InitSilo(string[] args)
        {
            hostWrapper = new OrleansHostWrapper(args);

            if (!hostWrapper.Run())
                Console.Error.WriteLine("Failed to initialize Orleans silo");
        }

        static void ShutdownSilo()
        {
            if (hostWrapper != null)
            {
                hostWrapper.Dispose();
                GC.SuppressFinalize(hostWrapper);
            }
        }

        private static OrleansHostWrapper hostWrapper;
    }
}
