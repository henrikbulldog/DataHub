using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataHub.Entities
{
    public class DataPoint
    {
        public string Id { get; set; }
        public string Source { get; set; }
        public long TimeSeriesId { get; set; }
        public DateTime Timestamp { get; set; }
        public string Value { get; set; }
    }
}
