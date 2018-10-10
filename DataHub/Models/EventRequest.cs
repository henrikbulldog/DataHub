using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataHub.Models
{
    public class EventRequest
    {
        /// <summary>
        /// Originating data source
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// Name of event
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Severity of event
        /// </summary>
        public string Severity { get; set; }

        /// <summary>
        /// Event creation time
        /// </summary>
        public DateTime Time { get; set; }

        /// <summary>
        /// Event payload
        /// </summary>
        public string Payload { get; set; }
    }
}
