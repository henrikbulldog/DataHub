using System;
using System.Collections.Generic;
using System.Text;

namespace DataHub.Entities
{
    public interface IEntity
    {
        /// <summary>
        /// Originating data source
        /// </summary>
        string Source { get; set; }

        /// <summary>
        /// Identifier
        /// </summary>
        string Id { get; set; }
    }
}
