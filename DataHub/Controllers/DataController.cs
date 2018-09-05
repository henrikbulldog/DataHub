using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using DataHub.Models;
using DataHub.Repositories;
using RepositoryFramework.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Community.OData.Linq.AspNetCore;
using Community.OData.Linq;

namespace DataHub.Controllers
{
    [Route("[controller]")]
    public class DataController : Controller
    {
        private IEntitiesRepository entitiesRepository;
        private DbContext dbContext;

        public DataController(
            IEntitiesRepository entitiesRepository,
            DbContext dbContext)
        {
            this.entitiesRepository = entitiesRepository;
            this.dbContext = dbContext;
        }

        [HttpGet("{entityName}")]
        public virtual IActionResult Get([FromRoute]string entityName, ODataQueryOptions queryOptions)
        {
            var entity = entitiesRepository.GetById(entityName);
            if (entity == null)
            {
                return NotFound($"No data found for entity {entityName}");
            }

            return Ok(GetType()
                .GetMethod("ApplyQueryOptions")
                .MakeGenericMethod(entity.ToType())
                .Invoke(this, new object[] { queryOptions }));
        }

        public IEnumerable<T> ApplyQueryOptions<T>(ODataQueryOptions queryOptions)
            where T : class
        {
            var repo = new EntityFrameworkRepository<T>(dbContext);
            return repo.AsQueryable().OData().ApplyQueryOptionsWithoutSelectExpand(queryOptions);
        }
    }
}
