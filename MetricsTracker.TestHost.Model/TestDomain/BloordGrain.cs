﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Orleans;
using Orleans.Runtime;

namespace MetricsTracker.TestHost.Model
{
    public class BloordGrain : Grain, IBloordGrain
    {
        Logger logger;
        Random rand;

        Guid LastFleebID;

        public override Task OnActivateAsync()
        {
            logger = GetLogger(nameof(BloordGrain));

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

            logger.IncrementMetric(Metrics.Poof);

            logger.TrackMetric(Metrics.RandomValue, rand.Next(1, 100));
        }
    }
}
