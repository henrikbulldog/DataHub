using System.Collections.Generic;

namespace DataHub.Models
{
    public class PagedListData<T>
    {
        public int? ItemsPerPage { get; set; }
        public int? StartIndex { get; set; }
        public int? TotalItems { get; set; }
        public int? PageIndex { get; set; }
        public int? TotalPages { get; set; }
        public IEnumerable<T> Items { get; set; }
    }
}