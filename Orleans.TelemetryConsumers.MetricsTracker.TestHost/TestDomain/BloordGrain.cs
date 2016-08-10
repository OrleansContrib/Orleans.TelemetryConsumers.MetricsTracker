using Orleans;
using Orleans.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetricsTracker.TestHost.TestDomain
{
    public class BloordGrain : Grain, IBloordGrain
    {
        Logger logger;
        Random rand;

        Guid LastFleebID;

        public override Task OnActivateAsync()
        {
            logger = GetLogger("BloordGrain");

            rand = new Random();

            return base.OnActivateAsync();
        }

        public async Task Poof(Guid fleebID)
        {
            LastFleebID = fleebID;

            logger.IncrementMetric("Poof");
        }
    }
}
