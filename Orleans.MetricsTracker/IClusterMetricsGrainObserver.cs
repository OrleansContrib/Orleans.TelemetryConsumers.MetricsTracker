﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orleans.MetricsTracker
{
    public interface IClusterMetricsGrainObserver : IGrainObserver
    {
        void Configure(MetricsConfiguration config);
        void DisableClusterMetrics();
    }
}
