using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataHub.Models
{
    /// <summary>
    /// Time series metadata
    /// </summary>
    public class TimeseriesMetadata : TimeseriesMetadataRequest
    {
        /// <summary>
        /// Primary key
        /// </summary>
        public string Id { get; set; }
    }
}
