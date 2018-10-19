using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace DataHub.Entities
{
    public class Asset
    {
        public string Id { get; set; }
        public string ParentId { get; set; }
        public string Source { get; set; }
        public string Tag { get; set; }
        public string Description { get; set; }
        public string SerialNumber { get; set; }
        public string Manufacturer { get; set; }
        public DateTime? Created { get; set; }
        public DateTime? Updated { get; set; }
        public IEnumerable<AssetTag> Tags { get; set; }
        public IEnumerable<TimeseriesMetadata> TimeSeries { get; set; }
        public IEnumerable<FileInfo> Files { get; set; }
        public IEnumerable<Asset> Assets { get; set; }
    }
}
