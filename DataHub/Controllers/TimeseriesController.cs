using Community.OData.Linq;
using Community.OData.Linq.AspNetCore;
using DataHub.Entities;
using DataHub.Hubs;
using DataHub.Models.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using RepositoryFramework.EntityFramework;
using RepositoryFramework.Interfaces;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DataHub.Controllers
{

    [Route("timeseries")]

#if RELEASE
    [Microsoft.AspNetCore.Authorization.Authorize]
#endif
    public class TimeseriesController : Controller
    {
        private ITimeseriesRepository timeseriesRepository;
        private IEntityFrameworkRepository<TimeseriesMetadata> timeseriesMetadataRepository;
        private IHubContext<TimeseriesHub> timerseriesHub;

        public TimeseriesController(
            ITimeseriesRepository timeseriesRepository,
            IEntityFrameworkRepository<TimeseriesMetadata> timeseriesMetadataRepository,
            IHubContext<TimeseriesHub> timerseriesHub)
        {
            this.timeseriesRepository = timeseriesRepository;
            this.timeseriesMetadataRepository = timeseriesMetadataRepository;
            this.timerseriesHub = timerseriesHub;
        }

        /// <summary>
        /// Create timeseries metadata
        /// </summary>
        /// <param name="items">List of timeseries metadata</param>
        /// <returns></returns>
        [HttpPost("metadata")]
        [ProducesResponseType(typeof(List<TimeseriesMetadata>), 201)]
        [ProducesResponseType(400)]
        public async virtual Task<IActionResult> PostMetadataAsync([FromBody]List<TimeseriesMetadata> items)
        {
            try
            {
                if (items == null || items.Count == 0)
                {
                    return BadRequest($"List of timeseries metadata must be specified");
                }

                var data = items.Select(i => new TimeseriesMetadata
                {
                    Id = string.IsNullOrEmpty(i.Id) ? Guid.NewGuid().ToString() : i.Id,
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
        /// <param name="orderby">Order items by property values, see [OData Sorting](http://docs.oasis-open.org/odata/odata/v4.0/odata-v4.0-part1-protocol.html#_Toc445374629)</param>
        /// <param name="filter">Filter items by property values, see [OData Filtering](http://docs.oasis-open.org/odata/odata/v4.0/odata-v4.0-part1-protocol.html#_Toc445374625)</param>
        /// <returns></returns>
        [HttpGet("metadata")]
        [ProducesResponseType(typeof(TimeseriesMetadataListResponse), 200)]
        public async virtual Task<IActionResult> GetMetadataAsync(
            [FromQuery(Name = "$top")] string top,
            [FromQuery(Name = "$skip")] string skip,
            [FromQuery(Name = "$orderby")] string orderby,
            [FromQuery(Name = "$filter")] string filter)
        {
            try
            {
                ODataQueryOptions oDataQueryOptions = new ODataQueryOptions
                {
                    Top = top,
                    Skip = skip,
                    OrderBy = orderby,
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
        /// <param name="source">Originating data source</param>
        /// <param name="id">Timeseries metadata id</param>
        /// <returns></returns>
        [HttpGet("metadata/{source}/{id}")]
        [ProducesResponseType(typeof(Entities.EventInfo), 200)]
        [ProducesResponseType(404)]
        public virtual async Task<IActionResult> GetMetadataByIdAsync(
            [FromRoute]string source, 
            [FromRoute]string id)
        {
            try
            {
                var entity = (await timeseriesMetadataRepository
                    .FindAsync(e => e.Source == source && e.Id == id)).FirstOrDefault();
                if (entity == null)
                {
                    return NotFound($"No data found for timeseries metadata id {id}");
                }

                return Ok(entity);
            }
            catch (Exception e)
            {
                return this.InternalServerError(e.FlattenMessages());
            }
        }

        /// <summary>
        /// Delete timeseries metadata
        /// </summary>
        /// <param name="source">Originating data source</param>
        /// <param name="id">Timeseries metadata id</param>
        /// <returns></returns>
        [HttpDelete("metadata/{source}/{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public virtual async Task<IActionResult> DeleteMetadataByIdAsync(
            [FromRoute]string source, 
            [FromRoute]string id)
        {
            try
            {
                var entity = (await timeseriesMetadataRepository
                    .FindAsync(e => e.Source == source && e.Id == id)).FirstOrDefault();
                if (entity == null)
                {
                    return NotFound($"No data found for timeseries metadata id {id}");
                }

                await timeseriesMetadataRepository.DeleteAsync(entity);

                return NoContent();
            }
            catch (Exception e)
            {
                return this.InternalServerError(e.FlattenMessages());
            }
        }

        /// <summary>
        /// Create timeseries datapoints. You can subscribe to datapoint creation, see instructions in /timeseries/subscriptionInfo.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [HttpPost()]
        [ProducesResponseType(201)]
        [ProducesResponseType(400)]
        public async virtual Task<IActionResult> PostAsync([FromBody]List<TimeseriesData> data)
        {
            try
            {
                if (data == null || data.Count == 0)
                {
                    return BadRequest($"Timeseries data must be specified");
                }

                await timeseriesRepository.CreateManyAsync(data);
                foreach (var ts in data)
                {
                    await PublishEventAsync(ts.Tag, JsonConvert.SerializeObject(
                        ts,
                        new JsonSerializerSettings
                        {
                            ContractResolver = new CamelCasePropertyNamesContractResolver()
                        }));
                }
                return Created(this.BuildLink(), null);
            }
            catch (Exception e)
            {
                return this.InternalServerError(e.FlattenMessages());
            }
        }

        /// <summary>
        /// Get timeseries datapoint subscription instructions
        /// </summary>
        /// <returns></returns>
        [HttpGet("/timeseries/subscriptionInfo")]
        [ProducesResponseType(typeof(Entities.EventSubscriptionInfo), 200)]
        public virtual IActionResult GetEventSubscriptionInfo()
        {
            try
            {
                return Ok(new Entities.EventSubscriptionInfo
                {
                    ConnectionUri = this.BuildLink(Startup.TIMESERIES_HUB_PATH),
                    Protocol = "SignalR",
                    MessageNamesLink = this.BuildLink($"/timeseries/metadata?$top=100"),
                    ClientDocumentationUri = "https://docs.microsoft.com/en-us/aspnet/core/signalr/clients?view=aspnetcore-2.1"
                });
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
        /// <param name="timeInterval">Aggregation time interval. Available values : raw, nanosecond, microsecond, millisecond, second, minute, hour, day, week, month, year</param>
        /// <param name="aggregationFunctions">Comma-spearated list of aggregation functions to use with group by or time interval. Available values : count, distinct, integral, mean, median, mode, spread, stddev, sum</param>
        /// <returns></returns>
        [HttpGet()]
        [ProducesResponseType(typeof(IEnumerable<TimeseriesData>), 200)]
        [ProducesResponseType(404)]
        public virtual async Task<IActionResult> GetAsync(
            [FromQuery]string tags,
            [FromQuery]string source,
            [FromQuery]DateTime? from,
            [FromQuery]DateTime? to,
            [FromQuery]string timeInterval = null,
            [FromQuery]string aggregationFunctions = null
            )
        {
            try
            {
                string[] taglist = { };
                if(!string.IsNullOrWhiteSpace(tags))
                {
                    tags = Regex.Replace(tags, @"\s+", "");
                    taglist = tags.Split(',');
                }

                var timeIntervalEnum = TimeInterval.raw;
                if(!string.IsNullOrWhiteSpace(timeInterval))
                {
                    timeInterval = Regex.Replace(timeInterval, @"\s+", "").ToLower();
                    Enum.TryParse<TimeInterval>(timeInterval, out timeIntervalEnum);
                }

                var aggregationFunctionEnums = new List<AggregationFunction>();
                if (!string.IsNullOrWhiteSpace(aggregationFunctions))
                {
                    aggregationFunctions = Regex.Replace(aggregationFunctions, @"\s+", "").ToLower();
                    var l = aggregationFunctions.Split(',');
                    foreach (var s in l)
                    {
                        AggregationFunction f;
                        if(Enum.TryParse<AggregationFunction>(s, out f))
                        {
                            aggregationFunctionEnums.Add(f);
                        }
                    }
                }

                IEnumerable<TimeseriesData> data;
                if(timeIntervalEnum == TimeInterval.raw)
                {
                    data = await timeseriesRepository.FindAsync(taglist, source, from, to);
                }
                else
                {
                    data = await timeseriesRepository.FindAggregateAsync(taglist, timeIntervalEnum, aggregationFunctionEnums, source, from, to);
                }
                return Ok(data);
            }
            catch (Exception e)
            {
                return this.InternalServerError(e.FlattenMessages());
            }
        }
        private async Task PublishEventAsync(string messageType, string message)
        {
            await timerseriesHub.Clients.All.SendAsync(messageType, message);
        }
    }
}