using System;
using System.Collections.Generic;
using System.Text;

namespace DataHub.Entities
{
    /// <summary>
    /// Actual products or parts
    /// </summary>
    public class SerialAsset
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string FunctionalAssetId { get; set; }
        public string SerialNumber { get; set; }
        public string Producer { get; set; }
        public IEnumerable<SerialAsset> SubAssets { get; set; }
    }
}
