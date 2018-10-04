using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataHub.Models
{
    public class FileInfo
    {
        public string Id { get; set; }
        public string Source { get; set; }
        public string Entity { get; set; }
        public string Filename { get; set; }
        public string Format { get; set; }
        public string DownloadUri { get; set; }
    }
}
