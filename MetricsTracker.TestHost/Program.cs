using System;
using System.Threading.Tasks;
using System.Threading;
using Orleans;
using Orleans.Streams;
using Orleans.Runtime.Configuration;
using MetricsTracker.TestDomain;

namespace Orleans.MetricsTracker.TestHost
{
    public class Program
    {
        static OrleansHostWrapper hostWrapper;

        static IStreamProvider StreamProvider;

        static IAsyncStream<MetricsSnapshot> ClusterSnapshotStream;
        //static IAsyncStream<MetricsSnapshot> SiloSnapshotStream;

        static void Main(string[] args)
        {
            Start(args).Wait();
        }

        static async Task Start(string[] args)
        {
            try
            {
                //var SyncContext = new SynchronizationContext();
                //SynchronizationContext.SetSynchronizationContext(SyncContext);

                // The Orleans silo environment is initialized in its own app domain in order to more
                // closely emulate the distributed situation, when the client and the server cannot
                // pass data via shared memory.
                var hostDomain = AppDomain.CreateDomain("OrleansHost", null, new AppDomainSetup
                {
                    AppDomainInitializer = InitSilo,
                    AppDomainInitializerArguments = args,
                });

                var config = ClientConfiguration.LocalhostSilo();
                config.AddSimpleMessageStreamProvider("SimpleStreamProvider");
                GrainClient.Initialize(config);

                // TODO: once the previous call returns, the silo is up and running.
                //       This is the place your custom logic, for example calling client logic
                //       or initializing an HTTP front end for accepting incoming requests.

                // configure and start the reporting of silo metrics
                // UNCOMMENT BELOW to override configuration defaults
                var metrics = GrainClient.GrainFactory.GetGrain<IClusterMetricsGrain>(Guid.Empty);
                await metrics.Configure(new MetricsConfiguration
                {
                    Enabled = true, // default
                    SamplingInterval = TimeSpan.FromSeconds(1), // default
                    ConfigurationInterval = TimeSpan.FromSeconds(10), // default
                    StaleSiloMetricsDuration = TimeSpan.FromSeconds(10), // default
                    LogExceptions = true,
                    TrackExceptionCounters = true,
                    TrackMethodGrainCalls = true,
                    StreamingProviderName = "SimpleStreamProvider",
                    HistoryLength = 30 // default
                });

                // TODO: put together a better demo
                // start our silly demo simulation
                var sim = GrainClient.GrainFactory.GetGrain<ISimulatorGrain>(Guid.Empty);
                await sim.StartSimulation(TimeSpan.FromMinutes(10), 200, 200, true);

                await SubscribeToStream();

                Console.WriteLine("Orleans Silo is running.\nPress Enter to terminate...");
                Console.ReadLine();

                hostDomain.DoCallBack(ShutdownSilo);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        static async Task SubscribeToStream()
        {
            StreamProvider = GrainClient.GetStreamProvider("SimpleStreamProvider");
            ClusterSnapshotStream = StreamProvider
                .GetStream<MetricsSnapshot>(Guid.Empty, "ClusterMetricSnapshots");

            await ClusterSnapshotStream.SubscribeAsync(OnNewMetricSnapshot);
        }

        static Task OnNewMetricSnapshot(MetricsSnapshot snapshot, StreamSequenceToken token)
        {
            Console.WriteLine(snapshot);

            return Task.CompletedTask;
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
    }
}
