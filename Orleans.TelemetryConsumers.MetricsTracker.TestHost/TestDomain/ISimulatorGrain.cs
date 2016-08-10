using System.Threading.Tasks;
using Orleans;
using System;

namespace MetricsTracker.TestHost.TestDomain
{
	public interface ISimulatorGrain : IGrainWithGuidKey
    {
        Task StartSimulation(TimeSpan duration, int fleebs, int bloords, bool fnord);
    }
}
