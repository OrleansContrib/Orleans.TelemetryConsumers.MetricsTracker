using MetricsTracker.TestHost.Model;
using Orleans;
using Orleans.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetricsTracker.TestDomain
{
    public class FleebGrain : Grain, IFleebGrain
    {
        Logger logger;
        Random rand;

        List<Guid> Bloords;

        public override Task OnActivateAsync()
        {
            logger = GetLogger(nameof(FleebGrain));

            rand = new Random();

            Bloords = new List<Guid>();
            for (int i = 0; i < 100; i++)
            {
                var id = Guid.NewGuid();
                Bloords.Add(id);
            }

            return base.OnActivateAsync();
        }

        public async Task Boop()
        {
            // TODO: uncomment this code to produce exceptions randomly
            //if (rand.NextDouble() < 0.0009)
            //    throw new ApplicationException("BLAM!");
            //else if (rand.NextDouble() < 0.0009)
            //    throw new InvalidOperationException("POW!");
            //else if (rand.NextDouble() < 0.0009)
            //    throw new NotFiniteNumberException("WHACK!");

            if (Bloords.Count < 1)
            {
                logger.IncrementMetric(Metrics.NothingToBoop);
                return;
            }

            for (int i = 0; i < 7; i++)
            {
                var index = rand.Next(0, Bloords.Count - 1);
                var bloordID = Bloords[index];

                var bloord = GrainFactory.GetGrain<IBloordGrain>(bloordID);
                await bloord.Poof(bloordID);

                logger.IncrementMetric(Metrics.Boop);
            }
        }
    }
}
