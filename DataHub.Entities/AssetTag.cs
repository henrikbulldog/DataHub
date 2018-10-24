using System;
using System.Collections.Generic;
using System.Text;

namespace DataHub.Entities
{
    public class AssetTag : IEntity
    {
        public string Source { get; set; }
        public string Id { get; set; }
        public string AssetId { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
    }
}
