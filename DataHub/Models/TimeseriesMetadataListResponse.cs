using DataHub.Entities;

namespace DataHub.Entities
{
    public class TimeseriesMetadataListResponse
    {
        public PagedListData<TimeseriesMetadata> Data { get; set; }
    }
}
