using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataHub.Entities
{
    public class TimeseriesMetadata : IComplexEntity
    {
        public string Source { get; set; }
        public string Id { get; set; }

        public string AssetId { get; set; }

        public string ParentSource { get; set; }
        public string ParentId { get; set; }

        /// <summary>
        /// Time series name or tag
        /// </summary>
        public string Name { get; set; }

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
        public DateTime? Created { get; set; }

        /// <summary>
        /// Updated
        /// </summary>
        public DateTime? Updated { get; set; }

        public IList<TimeseriesMetadata> Children { get; set; }
    }
}
