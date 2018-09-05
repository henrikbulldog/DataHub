using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataHub.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RepositoryFramework.Interfaces;

namespace DataHub.Controllers
{
    [Route("[controller]")]
    public class EntitiesController : Controller
    {
        private IEntitiesRepository entitiesRepository;

        public EntitiesController(
            IEntitiesRepository entitiesRepository)
        {
            this.entitiesRepository = entitiesRepository;
        }

        [HttpGet()]
        public virtual async Task<IActionResult> Get()
        {
            return Ok(await entitiesRepository.FindAsync());
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
    }
}
