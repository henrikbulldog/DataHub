using System;
using System.Collections.Generic;
using System.Text;

namespace DataHub.Entities
{
    public class Asset
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public IEnumerable<Asset> Assets { get; set; }
    }
}
