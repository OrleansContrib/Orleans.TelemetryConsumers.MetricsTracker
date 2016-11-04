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

        [TestMethod]
        public void TestOrleansClusterWithMetrics()
        {
            TestOrleansClusterWithMetricsAsync().Wait();
        }

        async Task TestOrleansClusterWithMetricsAsync()
        {
            var fleeb = Cluster.GrainFactory.GetGrain<IFleebGrain>(Guid.NewGuid());
            await fleeb.Boop();

            var blood = Cluster.GrainFactory.GetGrain<IBloordGrain>(Guid.NewGuid());
            await blood.Poof(Guid.NewGuid());

            var snapshot = await ClusterMetricsGrain.GetNextClusterMetrics(
                timeout: TimeSpan.FromSeconds(10));

            var poofCount = snapshot.Metrics.ContainsKey(Metrics.Poof) ? snapshot.Metrics["Poof"] : 0;

            Assert.IsNotNull(snapshot);
            Assert.IsTrue(snapshot.Metrics.ContainsKey("Poof"));
            Assert.AreEqual(1, snapshot.Metrics["Poof"]);
        }
    }
}
