using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Community.OData.Linq;
using Community.OData.Linq.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RepositoryFramework.Interfaces;

namespace DataHub.Controllers
{
    [Route("events")]
    public class EventsController : Controller
    {
        private IQueryableRepository<Models.EventInfo> eventsRepository;

        public EventsController(
            IQueryableRepository<Models.EventInfo> eventsRepository)
        {
            this.eventsRepository = eventsRepository;
        }

        [HttpGet("{id}")]
        public virtual async Task<IActionResult> Get([FromRoute]string id)
        {
            var e = await eventsRepository.GetByIdAsync(id);
            if (e == null)
            {
                return NotFound($"No data found for Event id {id}");
            }

            return Ok(e);
        }

        [HttpGet()]
        public virtual IActionResult Get(
            [FromQuery(Name = "$top")] string top,
            [FromQuery(Name = "$skip")] string skip,
            [FromQuery(Name = "$select")] string select,
            [FromQuery(Name = "$orderby")] string orderby,
            [FromQuery(Name = "$expand")] string expand,
            [FromQuery(Name = "$filter")] string filter)
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
            var events = eventsRepository
                .AsQueryable()
                .OData()
                .ApplyQueryOptionsWithoutSelectExpand(oDataQueryOptions);
            return Ok(events);
        }

        [HttpPost]
        public virtual async Task<IActionResult> Post([FromBody]Models.EventRequest EventRequest)
        {
            if (EventRequest == null)
            {
                return BadRequest("No Event data");
            }

            var id = Guid.NewGuid().ToString();
            var EventInfo = new Models.EventInfo
            {
                Id = id,
                Source = EventRequest.Source,
                Type = EventRequest.Type,
                Time = EventRequest.Time,
                Payload = EventRequest.Payload,
                Uri = this.BuildLink($"/Events/{id}")
            };
            await eventsRepository.CreateAsync(EventInfo);
            Request.HttpContext.Response.Headers.Add("Location", this.BuildLink($"/events/{id}"));
            return Ok(EventInfo);
        }
    }
}
