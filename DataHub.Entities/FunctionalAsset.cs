using System;
using System.Collections.Generic;
using System.Text;

namespace DataHub.Entities
{
    /// <summary>
    /// 
    /// </summary>
    public class FunctionalAsset
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string SiteId { get; set; }
        public string ReferenceAssetId { get; set; }
        public string TagNumber { get; set; }
        public string Location { get; set; }
        public IEnumerable<TimeSeriesTag> TimeSeriesTags { get; set; }
        public IEnumerable<FunctionalAsset> SubAssets { get; set; }
        public IEnumerable<SerialAsset> SerialAssets { get; set; }
    }
}