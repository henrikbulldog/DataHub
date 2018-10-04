using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataHub.Models
{
    public class DataListResponse
    {
        public PagedListData<object> Data { get; set; }
    }
}
