using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataHub.Entities
{
    public class EntityListResponse
    {
        public string SchemaVersion { get; set; }
        public PagedListData<Entity> Data { get; set; }
    }
}
