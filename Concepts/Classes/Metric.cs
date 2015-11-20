using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Concepts.Classes
{
    public class Metric
    {
        readonly public DateTimeOffset Created = DateTimeOffset.Now;

        public DateTimeOffset? End;

        public TimeSpan Difference { get { return End.HasValue ? End.Value - Created : TimeSpan.Zero; } }

        public Metric() { }

        public Metric(DateTimeOffset start) { Created = start; }
    }

    public class MetricLong : Metric
    {
        public long Counter;
    }
}
