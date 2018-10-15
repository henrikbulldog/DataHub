using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataHub.Entities
{
    /// <summary>
    /// File information
    /// </summary>
    public class FileInfo
    {
        /// <summary>
        /// Primary key
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string Source { get; set; }
        public string Entity { get; set; }
        public string Filename { get; set; }
        public string Format { get; set; }
        public string DownloadUri { get; set; }
    }
}
