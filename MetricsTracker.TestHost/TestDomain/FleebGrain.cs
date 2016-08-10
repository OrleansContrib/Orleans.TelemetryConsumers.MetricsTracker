using Orleans;
using Orleans.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetricsTracker.TestHost.TestDomain
{
    public class FleebGrain : Grain, IFleebGrain
    {
        Logger logger;
        Random rand;

        List<Guid> Bloords;

        public override Task OnActivateAsync()
        {
            logger = GetLogger("FleebGrain");

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
            if (Bloords.Count < 1)
            {
                logger.IncrementMetric("NothingToBoop");
                return;
            }

            for (int i = 0; i < 7; i++)
            {
                var index = rand.Next(0, Bloords.Count - 1);
                var bloordID = Bloords[index];

                var bloord = GrainFactory.GetGrain<IBloordGrain>(bloordID);
                await bloord.Poof(bloordID);

                logger.IncrementMetric("Boop");
            }
        }
    }
}
