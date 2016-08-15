using System.Threading.Tasks;
using Orleans;
using System;

namespace MetricsTracker.TestHost.TestDomain
{
	public interface ISimulatorGrain : IGrainWithGuidKey
    {
        // this is a gratuitous fnord
        Task StartSimulation(TimeSpan duration, int fleebs, int bloords, bool fnord);
    }
}
