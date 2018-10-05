using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataHub.Models
{
    public class FileListResponse
    {
        public PagedListData<FileInfo> Data { get; set; }
    }
}
