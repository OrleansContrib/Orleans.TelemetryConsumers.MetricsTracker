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

            // uncomment the following code to see how exceptions are counted:
            //   total exceptions reported
            //   unique exceptions reported
            //   specific counts per exception type

            // generate exceptions about 10% of the time
            //var rand = new Random(DateTime.UtcNow.Millisecond);
            //if (rand.NextDouble() < 0.1)
            //    throw new ApplicationException("RandomException");

            logger.IncrementMetric("Poof");
        }
    }
}
