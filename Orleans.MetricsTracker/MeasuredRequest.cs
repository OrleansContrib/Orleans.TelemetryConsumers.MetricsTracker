using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orleans.MetricsTracker
{
    public class MeasuredRequest
    {
        public string Name;
        public DateTimeOffset StartTime;
        public TimeSpan Duration;
    }
}
