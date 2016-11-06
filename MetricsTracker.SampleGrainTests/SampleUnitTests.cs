using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Orleans.TestingHost;
using Orleans.TelemetryConsumers.MetricsTracker;
using MetricsTracker.TestHost.Model;

namespace MetricsTracker.SampleGrainTests
{
    [TestClass]
    public class SampleUnitTests
    {
        static TestCluster Cluster;

        static IClusterMetricsGrain ClusterMetricsGrain;

        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            var options = new TestClusterOptions();

            options.ClusterConfiguration.Globals
                .RegisterBootstrapProvider<MetricsTrackerBootstrapProvider>("MetricsTracker");

            Cluster = new TestCluster(options);
            Cluster.Deploy();

            var metricsConfig = MetricsConfiguration.CreateUnitTestConfig();

            ClusterMetricsGrain = Cluster.GrainFactory.GetGrain<IClusterMetricsGrain>(Guid.Empty);
            ClusterMetricsGrain.Configure(metricsConfig).Wait();
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            Cluster.StopAllSilos();
        }

        [TestMethod]
        public async Task TestOrleansClusterWithMetrics()
        {
            var fleeb = Cluster.GrainFactory.GetGrain<IFleebGrain>(Guid.NewGuid());
            await fleeb.Boop();

            var snapshot = await ClusterMetricsGrain.GetNextClusterMetrics(
                timeout: TimeSpan.FromSeconds(10));

            Assert.IsNotNull(snapshot);
            Assert.IsTrue(snapshot.Metrics.ContainsKey(Metrics.Poof));
            Assert.AreEqual(7, snapshot.Metrics[Metrics.Poof]);
        }
    }
}
