using Community.OData.Linq;
using Community.OData.Linq.AspNetCore;
using DataHub.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RepositoryFramework.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataHub.Controllers
{

    [Route("timeseries")]
    public class TimeseriesController : Controller
    {
        private ITimeseriesRepository timeseriesRepository;
        private IQueryableRepository<TimeseriesMetadata> timeseriesMetadataRepository;

        public TimeseriesController(
            ITimeseriesRepository timeseriesRepository,
            IQueryableRepository<TimeseriesMetadata> timeseriesMetadataRepository)
        {
            this.timeseriesRepository = timeseriesRepository;
            this.timeseriesMetadataRepository = timeseriesMetadataRepository;
        }

        /// <summary>
        /// Create timeseries metadata
        /// </summary>
        /// <param name="items">List of timeseries metadata</param>
        /// <returns></returns>
        [HttpPost("metadata")]
        [ProducesResponseType(typeof(List<TimeseriesMetadata>), 201)]
        [ProducesResponseType(400)]
        public async virtual Task<IActionResult> PostMetadataAsync([FromBody]List<TimeseriesMetadataRequest> items)
        {
            try
            {
                if (items == null || items.Count == 0)
                {
                    return BadRequest($"List of timeseries metadata must be specified");
                }

                var data = items.Select(i => new TimeseriesMetadata
                {
                    Id = Guid.NewGuid().ToString(),
                    Created = i.Created,
                    Description = i.Description,
                    Name = i.Name,
                    Source = i.Source,
                    Units = i.Units,
                    Updated = i.Updated
                }).ToList();
                await timeseriesMetadataRepository.CreateManyAsync(data);
                return Created(this.BuildLink(), data);
            }
            catch (Exception e)
            {
                return this.InternalServerError(e.FlattenMessages());
            }
        }

        /// <summary>
        /// Get list of timeseries metadata
        /// </summary>
        /// <param name="top">Show only the first n items, see [OData Paging - Top](http://docs.oasis-open.org/odata/odata/v4.0/odata-v4.0-part1-protocol.html#_Toc445374630)</param>
        /// <param name="skip">Skip the first n items, see [OData Paging - Skip](http://docs.oasis-open.org/odata/odata/v4.0/odata-v4.0-part1-protocol.html#_Toc445374631)</param>
        /// <param name="select"> Select properties to be returned, see [OData Select](http://docs.oasis-open.org/odata/odata/v4.0/odata-v4.0-part1-protocol.html#_Toc445374620)</param>
        /// <param name="orderby">Order items by property values, see [OData Sorting](http://docs.oasis-open.org/odata/odata/v4.0/odata-v4.0-part1-protocol.html#_Toc445374629)</param>
        /// <param name="expand">Expand object and collection properties, see [OData Expand](http://docs.oasis-open.org/odata/odata/v4.0/errata03/os/complete/part1-protocol/odata-v4.0-errata03-os-part1-protocol-complete.html#_System_Query_Option_6)</param>
        /// <param name="filter">Filter items by property values, see [OData Filtering](http://docs.oasis-open.org/odata/odata/v4.0/odata-v4.0-part1-protocol.html#_Toc445374625)</param>
        /// <returns></returns>
        [HttpGet("metadata")]
        [ProducesResponseType(typeof(TimeseriesMetadataListResponse), 200)]
        public async virtual Task<IActionResult> GetMetadataAsync(
            [FromQuery(Name = "$top")] string top,
            [FromQuery(Name = "$skip")] string skip,
            [FromQuery(Name = "$select")] string select,
            [FromQuery(Name = "$orderby")] string orderby,
            [FromQuery(Name = "$expand")] string expand,
            [FromQuery(Name = "$filter")] string filter)
        {
            try
            {
                ODataQueryOptions oDataQueryOptions = new ODataQueryOptions
                {
                    Top = top,
                    Skip = skip,
                    Select = select,
                    OrderBy = orderby,
                    Expand = expand,
                    Filters = string.IsNullOrEmpty(filter) ? null : new List<string> { filter }
                };

                var entities = await Task.FromResult(timeseriesMetadataRepository
                    .AsQueryable()
                    .OData()
                    .ApplyQueryOptionsWithoutSelectExpand(oDataQueryOptions)
                    .ToList());
                return Ok(new TimeseriesMetadataListResponse
                {
                    Data = await PagedListData<TimeseriesMetadata>.CreateAsync(
                        entities, 
                        top, 
                        skip, 
                        async () => await timeseriesMetadataRepository.AsQueryable().LongCountAsync())
                });
            }
            catch (Exception e)
            {
                return this.InternalServerError(e.FlattenMessages());
            }
        }

        /// <summary>
        /// Get timeseries metadata by id
        /// </summary>
        /// <param name="id">Timeseries metadata id</param>
        /// <returns></returns>
        [HttpGet("metadata/{id}")]
        [ProducesResponseType(typeof(Models.EventInfo), 200)]
        [ProducesResponseType(404)]
        public virtual async Task<IActionResult> GetMetadataByIdAsync([FromRoute]string id)
        {
            try
            {
                var e = await timeseriesMetadataRepository.GetByIdAsync(id);
                if (e == null)
                {
                    return NotFound($"No data found for timeseries metadata id {id}");
                }

                return Ok(e);
            }
            catch (Exception e)
            {
                return this.InternalServerError(e.FlattenMessages());
            }
        }

        /// <summary>
        /// Query timeseries 
        /// </summary>
        /// <param name="tags">Comma separated list of tags</param>
        /// <param name="source">Originating data source</param>
        /// <param name="from">From</param>
        /// <param name="to">To</param>
        /// <param name="timeInterval">Aggregation time interval
        /// <param name="aggregationFunctions">Aggregation function to use with group by or time interval
        /// <returns></returns>
        [HttpGet()]
        [ProducesResponseType(typeof(IEnumerable<TimeseriesData>), 200)]
        [ProducesResponseType(404)]
        public virtual async Task<IActionResult> GetAsync(
            [FromQuery]string tags,
            [FromQuery]string source,
            [FromQuery]DateTime? from,
            [FromQuery]DateTime? to,
            [FromQuery]TimeInterval timeInterval,
            [FromQuery]IList<AggregationFunction> aggregationFunctions
            )
        {
            try
            {
                IEnumerable<TimeseriesData> data;
                if(timeInterval == TimeInterval.Raw)
                {
                    data = await timeseriesRepository.FindAsync(tags.Split(','), source, from, to);
                }
                else
                {
                    data = await timeseriesRepository.FindAggregateAsync(tags.Split(','), timeInterval, aggregationFunctions, source, from, to);
                }
                return Ok(data);
            }
            catch (Exception e)
            {
                return this.InternalServerError(e.FlattenMessages());
            }
        }
    }
}