﻿using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetricsTracker.TestHost.Model
{
    public interface IBloordGrain : IGrainWithGuidKey
    {
        Task Poof(Guid fleebID);
    }
}
