using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using Community.OData.Linq;
using Community.OData.Linq.AspNetCore;
using DataHub.Hubs;
using DataHub.Models.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using RepositoryFramework.Interfaces;

namespace DataHub.Controllers
{
    [Route("events")]
#if RELEASE
    [Microsoft.AspNetCore.Authorization.Authorize]
#endif
    public class EventsController : Controller
    {
        private IQueryableRepository<Entities.EventInfo> eventsRepository;
        private IHubContext<EventHub> eventHub;

        public EventsController(
            IQueryableRepository<Entities.EventInfo> eventsRepository,
            IHubContext<EventHub> eventHub)
        {
            this.eventsRepository = eventsRepository;
            this.eventHub = eventHub;
        }

        /// <summary>
        /// Get an event by id
        /// </summary>
        /// <param name="id">Event id</param>
        /// <returns></returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Entities.EventInfo), 200)]
        [ProducesResponseType(404)]
        public virtual async Task<IActionResult> GetByIdAsync([FromRoute]string id)
        {
            try
            {
                var e = await eventsRepository.GetByIdAsync(id);
                if (e == null)
                {
                    return NotFound($"No data found for Event id {id}");
                }

                return Ok(e);
            }
            catch (Exception e)
            {
                return this.InternalServerError(e.FlattenMessages());
            }
        }

        /// <summary>
        /// Get a list of events
        /// </summary>
        /// <param name="top">Show only the first n items, see [OData Paging - Top](http://docs.oasis-open.org/odata/odata/v4.0/odata-v4.0-part1-protocol.html#_Toc445374630)</param>
        /// <param name="skip">Skip the first n items, see [OData Paging - Skip](http://docs.oasis-open.org/odata/odata/v4.0/odata-v4.0-part1-protocol.html#_Toc445374631)</param>
        /// <param name="select"> Select properties to be returned, see [OData Select](http://docs.oasis-open.org/odata/odata/v4.0/odata-v4.0-part1-protocol.html#_Toc445374620)</param>
        /// <param name="orderby">Order items by property values, see [OData Sorting](http://docs.oasis-open.org/odata/odata/v4.0/odata-v4.0-part1-protocol.html#_Toc445374629)</param>
        /// <param name="expand">Expand object and collection properties, see [OData Expand](http://docs.oasis-open.org/odata/odata/v4.0/errata03/os/complete/part1-protocol/odata-v4.0-errata03-os-part1-protocol-complete.html#_System_Query_Option_6)</param>
        /// <param name="filter">Filter items by property values, see [OData Filtering](http://docs.oasis-open.org/odata/odata/v4.0/odata-v4.0-part1-protocol.html#_Toc445374625)</param>
        /// <returns></returns>
        [HttpGet()]
        [ProducesResponseType(typeof(Entities.EventListResponse), 200)]
        public async virtual Task<IActionResult> GetAsync(
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
                var events = await Task.FromResult(eventsRepository
                    .AsQueryable()
                    .OData()
                    .ApplyQueryOptions(oDataQueryOptions)
                    .Select(e => e.ToDictionary().ToObject<Entities.EventInfo>())
                    .ToList());
                return Ok(new Entities.EventListResponse
                {
                    Data = await Entities.PagedListData<Entities.EventInfo>.CreateAsync(
                        events,
                        top,
                        skip,
                        async () => await eventsRepository.AsQueryable().LongCountAsync())
                });
            }
            catch (Exception e)
            {
                return this.InternalServerError(e.FlattenMessages());
            }
        }

        /// <summary>
        /// Create an event. You can subscribe to event creation, see instructions in /events/subscriptionInfo.
        /// </summary>
        /// <param name="eventRequest">Event request</param>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(typeof(Entities.EventInfo), 201)]
        [ProducesResponseType(400)]
        public virtual async Task<IActionResult> PostAsync([FromBody]Entities.EventInfo eventRequest)
        {
            try
            {
                if (eventRequest == null)
                {
                    return BadRequest("No Event data");
                }

                var eventInfo = new Entities.EventInfo
                {
                    Id = string.IsNullOrEmpty(eventRequest.Id) ? Guid.NewGuid().ToString() : eventRequest.Id,
                    Source = eventRequest.Source,
                    Name = eventRequest.Name,
                    Time = eventRequest.Time,
                    Payload = eventRequest.Payload
                };
                await eventsRepository.CreateAsync(eventInfo);
                await PublishEventAsync(eventRequest.Name, JsonConvert.SerializeObject(eventInfo));
                return Created(this.BuildLink($"/events/{eventInfo.Id}"), eventInfo);
            }
            catch (Exception e)
            {
                return this.InternalServerError(e.FlattenMessages());
            }
        }

        /// <summary>
        /// Get event subscription instructions
        /// </summary>
        /// <returns></returns>
        [HttpGet("/events/subscriptionInfo")]
        [ProducesResponseType(typeof(Entities.EventSubscriptionInfo), 200)]
        public virtual IActionResult GetEventSubscriptionInfo()
        {
            try
            {
                return Ok(new Entities.EventSubscriptionInfo
                {
                    ConnectionUri = this.BuildLink(Startup.EVENT_HUB_PATH),
                    Protocol = "SignalR",
                    MessageNamesLink = this.BuildLink($"/events?$top=100&$select=name"),
                    ClientDocumentationUri = "https://docs.microsoft.com/en-us/aspnet/core/signalr/clients?view=aspnetcore-2.1"
                });
            }
            catch (Exception e)
            {
                return this.InternalServerError(e.FlattenMessages());
            }
        }

        private async Task PublishEventAsync(string messageType, string message)
        {
            await eventHub.Clients.All.SendAsync(messageType, message);
        }
    }
}
