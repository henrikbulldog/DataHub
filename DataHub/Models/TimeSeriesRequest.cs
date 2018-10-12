using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataHub.Models
{
    /// <summary>
    /// Time series metadata
    /// </summary>
    public class TimeseriesMetadataRequest
    {
        /// <summary>
        /// Time series name or tag
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Originating data source
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// Description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Units
        /// </summary>
        public string Units { get; set; }

        /// <summary>
        /// Created
        /// </summary>
        public DateTime Created { get; set; }

        /// <summary>
        /// Updated
        /// </summary>
        public DateTime Updated { get; set; }
    }
}
