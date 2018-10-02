using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Community.OData.Linq;
using Community.OData.Linq.AspNetCore;
using DataHub.Models;
using DataHub.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RepositoryFramework.EntityFramework;
using RepositoryFramework.Interfaces;

namespace DataHub.Controllers
{
    [Route("[controller]")]
    public class EntitiesController : Controller
    {
        private IEntitiesRepository entitiesRepository;
        private DbContext dbContext;

        public EntitiesController(
            IEntitiesRepository entitiesRepository,
            DbContext dbContext)
        {
            this.entitiesRepository = entitiesRepository;
            this.dbContext = dbContext;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="top"></param>
        /// <param name="skip"></param>
        /// <param name="select"></param>
        /// <param name="orderby"></param>
        /// <param name="expand"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        [HttpGet()]
        public virtual IActionResult Get(
            [FromQuery(Name ="$top")] string top,
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
                Filters = new List<string> { filter }
            };

            var events = entitiesRepository
                .AsQueryable()
                .OData()
                .ApplyQueryOptionsWithoutSelectExpand(oDataQueryOptions);
            return Ok(events);
        }

        [HttpGet("{name}")]
        public virtual async Task<IActionResult> Get([FromRoute]string name)
        {
            var entity = await entitiesRepository.GetByIdAsync(name);
            if (entity == null)
            {
                return NotFound($"No data found for entity {name}");
            }

            return Ok(entity);
        }

        [HttpGet("{entityName}/data")]
        public virtual IActionResult GetData([FromRoute]string entityName, ODataQueryOptions oDataQueryOptions)
        {
            var entity = entitiesRepository.GetById(entityName);
            if (entity == null)
            {
                return NotFound($"No data found for entity {entityName}");
            }

            return Ok(GetType()
                .GetMethod("ApplyQueryOptions")
                .MakeGenericMethod(entity.ToType())
                .Invoke(this, new object[] { oDataQueryOptions }));
        }

        public IEnumerable<T> ApplyQueryOptions<T>(ODataQueryOptions queryOptions)
            where T : class
        {
            var repo = new EntityFrameworkRepository<T>(dbContext);
            return repo.AsQueryable()
                .OData()
                .ApplyQueryOptionsWithoutSelectExpand(queryOptions);
        }
    }
}
