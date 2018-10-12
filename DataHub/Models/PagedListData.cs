using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataHub.Models
{
    public class PagedListData<T>
    {
        /// <summary>
        /// The number of items in the result. 
        /// This is not necessarily the size of the data.items array; 
        /// if we are viewing the last page of items, 
        /// the size of data.items may be less than itemsPerPage. 
        /// However the size of data.items should not exceed itemsPerPage.
        /// </summary>
        public long? ItemsPerPage { get; set; }

        /// <summary>
        /// The index of the first item in data.items. 
        /// For consistency, startIndex should be 1-based. 
        /// For example, the first item in the first set of items should have a startIndex of 1. 
        /// If the user requests the next set of data, the startIndex may be 10.
        /// </summary>
        public long? StartIndex { get; set; }

        /// <summary>
        /// The total number of items available in this set. 
        /// For example, if a user has 100 blog posts, the response may only contain 10 items, but the totalItems would be 100.
        /// </summary>
        public long? TotalItems { get; set; }

        /// <summary>
        /// The index of the current page of items. 
        /// For consistency, pageIndex should be 1-based. 
        /// For example, the first page of items has a pageIndex of 1. 
        /// pageIndex can also be calculated from the item-based paging properties: pageIndex = floor(startIndex / itemsPerPage) + 1.
        /// </summary>
        public long? PageIndex { get; set; }

        /// <summary>
        /// The total number of pages in the result set. 
        /// totalPages can also be calculated from the item-based paging properties above: totalPages = ceiling(totalItems / itemsPerPage).
        /// </summary>
        public long? TotalPages { get; set; }

        /// <summary>
        /// The property name items is reserved to represent an array of items (for example, photos in Picasa, videos in YouTube). 
        /// This construct is intended to provide a standard location for collections related to the current result. 
        /// For example, the JSON output could be plugged into a generic pagination system that knows to page on the items array. 
        /// If items exists, it should be the last property in the data object (See the "Property Ordering" section below for more details).
        /// </summary>
        public IEnumerable<T> Items { get; set; }

        public static async Task<PagedListData<T>> CreateAsync(
            IEnumerable<T> items,
            string top = null,
            string skip = null,
            Func<Task<long?>> getTotalItems = null)
        {
            var r = new PagedListData<T>();
            r.Items = items;
            int t;
            if (int.TryParse(top, out t))
            {
                r.ItemsPerPage = t;
                r.TotalItems = getTotalItems != null ? await getTotalItems() : null;
                r.TotalPages = r.TotalItems.Value / r.ItemsPerPage.Value;
                int s;
                if (int.TryParse(skip, out s))
                {
                    r.StartIndex = s;
                    r.PageIndex = (t / s) + 1;
                }
            }

            return r;
        }

    }
}