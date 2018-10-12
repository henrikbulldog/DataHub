using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataHub.Models
{
    public class TimeseriesMetadataListResponse
    {
        public PagedListData<TimeseriesMetadata> Data { get; set; }
    }
}
