using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataHub.Models
{
    public class EventListResponse
    {
        public PagedListData<EventInfo> Data { get; set; }
    }
}
