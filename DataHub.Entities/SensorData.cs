using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataHub.Entities
{
    public class SensorData
    {
        public string Id { get; set; }
        public string Source { get; set; }
        public string TagId { get; set; }
        public DateTime Timestamp { get; set; }
        public string Value { get; set; }
        public string Units { get; set; }
        public Tag Tag { get; set; }
    }
}
