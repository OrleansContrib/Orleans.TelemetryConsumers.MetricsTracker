﻿using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetricsTracker.TestDomain
{
    public interface IFleebGrain : IGrainWithGuidKey
    {
        Task Boop();
    }
}
