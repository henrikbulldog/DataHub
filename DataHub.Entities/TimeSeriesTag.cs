using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataHub.Entities
{
    public class TimeSeriesTag
    {
        public string Id { get; set; }
        /// <summary>
        /// Original equipment manufacturer tag name
        /// </summary>
        public string OEMTagName { get; set; }
        public string Name { get; set; }
        public string Units { get; set; }
    }
}
