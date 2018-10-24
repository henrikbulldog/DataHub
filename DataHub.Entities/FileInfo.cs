using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataHub.Entities
{
    /// <summary>
    /// File information
    /// </summary>
    public class FileInfo : IEntity
    {
        public string Source { get; set; }
        public string Id { get; set; }
        public string AssetId { get; set; }
        public string Filename { get; set; }
        public string Format { get; set; }
        public string DownloadUri { get; set; }
    }
}
