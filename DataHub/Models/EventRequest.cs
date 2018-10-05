using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataHub.Models
{
    public class EventRequest
    {
        public string Source { get; set; }
        public string Type { get; set; }
        public DateTime Time { get; set; }
        public object Payload { get; set; }
    }
}
