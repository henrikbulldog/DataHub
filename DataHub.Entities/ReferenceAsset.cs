using System;
using System.Collections.Generic;
using System.Text;

namespace DataHub.Entities
{
    /// <summary>
    /// Refence model for all sites based on the SFI standard
    /// </summary>
    public class ReferenceAsset
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string SFITag { get; set; }
        public IEnumerable<ReferenceAsset> SubAssets { get; set; }
        public IEnumerable<FunctionalAsset> FunctionalAssets { get; set; }
    }
}
