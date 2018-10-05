using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataHub.Entities
{
    public class TimeSeries
    {
        public long Id { get; set; }
        public string Source { get; set; }
        public long TimeSeriesTagId { get; set; }
        public DateTime Timestamp { get; set; }
        public string Value { get; set; }
        public TimeSeriesTag Tag { get; set; }
    }
}
