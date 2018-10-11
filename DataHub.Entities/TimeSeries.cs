using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataHub.Entities
{
    public class TimeSeries
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Source { get; set; }
        public string Description { get; set; }
        public string Units { get; set; }
        public DateTime Created { get; set; }
        public DateTime Updated { get; set; }
    }
}
