using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetricsTracker.TestHost.TestDomain
{
    public class SimulatorGrain : Grain, ISimulatorGrain
    {
        IDisposable SimulationTimer;

        List<Guid> Fleebs;

        Random random;

        DateTime StartTime;
        TimeSpan Duration;

        public Task StartSimulation(TimeSpan duration, int fleebs, int bloords, bool fnord)
        {
            if (SimulationTimer != null)
                throw new InvalidOperationException("The simulator is already running.");

            random = new Random();

            StartTime = DateTime.UtcNow;
            Duration = duration;

            Fleebs = new List<Guid>();
            for (int i = 0; i < 200; i++)
                Fleebs.Add(Guid.NewGuid());

            SimulationTimer = RegisterTimer(RunSimulation, null,
                dueTime: TimeSpan.Zero,
                period: TimeSpan.FromSeconds(1));

            return TaskDone.Done;
        }

        async Task RunSimulation(object state)
        {
            // disable the timer when the specified Duration has passed
            if (DateTime.UtcNow.Subtract(StartTime) >= Duration)
            {
                SimulationTimer.Dispose();
                return;
            }

            // pick between 10 and 50 fleebs
            var activeFleebCount = random.Next(10, 50);

            // do some booping, which only fleebs can do
            for (int i = 0; i < activeFleebCount; i++)
            {
                var index = random.Next(0, Fleebs.Count - 1);
                var fleebID = Fleebs[index];

                var fleebGrain = GrainFactory.GetGrain<IFleebGrain>(fleebID);
                await fleebGrain.Boop();
            }
        }
    }
}
