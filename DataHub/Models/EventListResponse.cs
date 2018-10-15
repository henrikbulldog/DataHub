using DataHub.Entities;

namespace DataHub.Entities
{
    public class EventListResponse
    {
        public PagedListData<EventInfo> Data { get; set; }
    }
}
