using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Orleans.TestingHost;
using Orleans.TelemetryConsumers.MetricsTracker;
using MetricsTracker.TestDomain;
using System.Threading.Tasks;

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

            ClusterMetricsGrain = Cluster.GrainFactory.GetGrain<IClusterMetricsGrain>(Guid.Empty);
            ClusterMetricsGrain.Configure(new MetricsConfiguration
            {
                SamplingInterval = TimeSpan.FromMilliseconds(1),
                ConfigurationInterval = TimeSpan.FromDays(1)
            }).Wait();
        }

        [TestMethod]
        public void TestOrleansClusterCounters()
        {
            TestOrleansClusterWithMetrics().Wait();
        }

        async Task TestOrleansClusterWithMetrics()
        {
            var blood = Cluster.GrainFactory.GetGrain<IBloordGrain>(Guid.NewGuid());
            await blood.Poof(Guid.NewGuid());

            await Task.Delay(500);

            var snapshot = await ClusterMetricsGrain.GetClusterMetrics();

            var poofCount = snapshot.Metrics.ContainsKey("Poof") ? snapshot.Metrics["Poof"] : 0;

            Assert.AreEqual(1, poofCount);
        }
    }
}
