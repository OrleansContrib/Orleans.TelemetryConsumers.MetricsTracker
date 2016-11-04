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

        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            var options = new TestClusterOptions();

            options.ClusterConfiguration.Globals
                .RegisterBootstrapProvider<MetricsTrackerBootstrapProvider>("MetricsTracker");

            Cluster = new TestCluster(options);
            Cluster.Deploy();
        }

        [TestMethod]
        public void TestOrleansClusterCounters()
        {
            var x = Cluster.GrainFactory.GetGrain<IBloordGrain>(Guid.NewGuid());
            x.Poof(Guid.NewGuid()).Wait();
            //Cannot find generated GrainReference class 
            //for interface 'MetricsTracker.TestHost.TestDomain.IBloordGrain'

            Task.Delay(2000).Wait();

            var metricsGrain = Cluster.GrainFactory.GetGrain<IClusterMetricsGrain>(Guid.Empty);
            var snapshot = metricsGrain.GetClusterMetrics().Result;

            var poofCount = snapshot.Metrics.ContainsKey("Poof") ? snapshot.Metrics["Poof"] : 0;

            Assert.AreEqual(1, poofCount);
        }
    }
}
