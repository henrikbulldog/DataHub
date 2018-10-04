using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Community.OData.Linq;
using Community.OData.Linq.AspNetCore;
using DataHub.Models;
using DataHub.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using RepositoryFramework.EntityFramework;
using RepositoryFramework.Interfaces;

namespace DataHub.Controllers
{
    [Route("entities")]
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
        /// Get list of entity schema information
        /// </summary>
        /// <param name="top">Show only the first n items, see [OData Paging - Top](http://docs.oasis-open.org/odata/odata/v4.0/odata-v4.0-part1-protocol.html#_Toc445374630)</param>
        /// <param name="skip">Skip the first n items, see [OData Paging - Skip](http://docs.oasis-open.org/odata/odata/v4.0/odata-v4.0-part1-protocol.html#_Toc445374631)</param>
        /// <param name="select"> Select properties to be returned, see [OData Select](http://docs.oasis-open.org/odata/odata/v4.0/odata-v4.0-part1-protocol.html#_Toc445374620)</param>
        /// <param name="orderby">Order items by property values, see [OData Sorting](http://docs.oasis-open.org/odata/odata/v4.0/odata-v4.0-part1-protocol.html#_Toc445374629)</param>
        /// <param name="expand">Expand object and collection properties, see [OData Expand](http://docs.oasis-open.org/odata/odata/v4.0/errata03/os/complete/part1-protocol/odata-v4.0-errata03-os-part1-protocol-complete.html#_System_Query_Option_6)</param>
        /// <param name="filter">Filter items by property values, see [OData Filtering](http://docs.oasis-open.org/odata/odata/v4.0/odata-v4.0-part1-protocol.html#_Toc445374625)</param>
        /// <returns></returns>
        [HttpGet()]
        [ProducesResponseType(typeof(EntityListResponse), 200)]
        public virtual IActionResult GetEntities(
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
                Filters = string.IsNullOrEmpty(filter) ? null : new List<string> { filter }
            };

            var entities = entitiesRepository
                .AsQueryable()
                .OData()
                .ApplyQueryOptionsWithoutSelectExpand(oDataQueryOptions);
            return Ok(new EntityListResponse
            {
                SchemaVersion = Assembly.GetAssembly(typeof(DataHub.Entities.Site)).GetName().Version.ToString(),
                Data = new PagedListData<Entity>
                {
                    Items = entities
                }
            });
        }

        /// <summary>
        /// Get schema information of a single entity
        /// </summary>
        /// <param name="name">Data entity or type of document</param>
        /// <returns></returns>
        [HttpGet("{name}")]
        [ProducesResponseType(typeof(Entity), 200)]
        [ProducesResponseType(404)]
        public virtual async Task<IActionResult> GetEntityByName([FromRoute]string name)
        {
            var entity = await entitiesRepository.GetByIdAsync(name);
            if (entity == null)
            {
                return NotFound($"No data found for entity {name}");
            }

            return Ok(entity);
        }

        /// <summary>
        /// Get a list of entity data items
        /// </summary>
        /// <param name="name">Data entity or type of document</param>
        /// <param name="top">Show only the first n items, see [OData Paging - Top](http://docs.oasis-open.org/odata/odata/v4.0/odata-v4.0-part1-protocol.html#_Toc445374630)</param>
        /// <param name="skip">Skip the first n items, see [OData Paging - Skip](http://docs.oasis-open.org/odata/odata/v4.0/odata-v4.0-part1-protocol.html#_Toc445374631)</param>
        /// <param name="select"> Select properties to be returned, see [OData Select](http://docs.oasis-open.org/odata/odata/v4.0/odata-v4.0-part1-protocol.html#_Toc445374620)</param>
        /// <param name="orderby">Order items by property values, see [OData Sorting](http://docs.oasis-open.org/odata/odata/v4.0/odata-v4.0-part1-protocol.html#_Toc445374629)</param>
        /// <param name="expand">Expand object and collection properties, see [OData Expand](http://docs.oasis-open.org/odata/odata/v4.0/errata03/os/complete/part1-protocol/odata-v4.0-errata03-os-part1-protocol-complete.html#_System_Query_Option_6)</param>
        /// <param name="filter">Filter items by property values, see [OData Filtering](http://docs.oasis-open.org/odata/odata/v4.0/odata-v4.0-part1-protocol.html#_Toc445374625)</param>
        /// <returns></returns>
        [HttpGet("{name}/data")]
        [ProducesResponseType(typeof(DataListResponse), 200)]
        [ProducesResponseType(404)]
        public virtual IActionResult GetData(
            [FromRoute]string name,
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

            var entity = entitiesRepository.GetById(name);
            if (entity == null)
            {
                return NotFound($"No data found for entity {name}");
            }

            var items = GetType()
                .GetMethod("ApplyQueryOptions")
                .MakeGenericMethod(entity.ToType())
                .Invoke(this, new object[] { oDataQueryOptions }) as IEnumerable<object>;

            return Ok(new DataListResponse
            {
                Data = new PagedListData<object>
                {
                    Items = items
                }
            });
        }

        public IEnumerable<T> ApplyQueryOptions<T>(ODataQueryOptions queryOptions)
            where T : class
        {
            var repo = new EntityFrameworkRepository<T>(dbContext);
            return repo.AsQueryable()
                .OData()
                .ApplyQueryOptionsWithoutSelectExpand(queryOptions);
        }

        /// <summary>
        /// Create a single entity data item
        /// </summary>
        /// <param name="name">Data entity or type of document</param>
        /// <param name="item">Entity object</param>
        /// <returns></returns>
        [HttpPost("{name}/data")]
        [ProducesResponseType(typeof(object), 201)]
        [ProducesResponseType(404)]
        public virtual IActionResult PostData(
            [FromRoute]string name, 
            [FromBody]dynamic item)
        {
            var entity = entitiesRepository.GetById(name);
            if (entity == null)
            {
                return NotFound($"Unknown entity {name}");
            }

            var newItem = GetType()
                .GetMethod("Create")
                .MakeGenericMethod(entity.ToType())
                .Invoke(this, new object[] { item });

            string uri = this.BuildLink($"/entities/{name}");
            var idprop = newItem.GetType().GetProperty("Id");
            if (idprop != null && newItem != null)
            {
                uri += $"/{idprop.GetValue(newItem)}";
            }
            return Created(uri, newItem);
        }

        public T Create<T>(dynamic item)
            where T : class, new()
        {
            var repo = new EntityFrameworkRepository<T>(dbContext);
            var newItem = JsonConvert.DeserializeObject<T>(
                JsonConvert.SerializeObject(item));
            repo.Create(newItem);
            repo.SaveChanges();
            return newItem;
        }

        /// <summary>
        /// Get a single entity data item
        /// </summary>
        /// <param name="name">Data entity or type of document</param>
        /// <param name="id">Id</param>
        /// <returns></returns>
        [HttpGet("{name}/data/{id}")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(404)]
        public virtual async Task<IActionResult> GetDataById([FromRoute]string name, [FromRoute]string id)
        {
            var entity = await entitiesRepository.GetByIdAsync(name);
            if (entity == null)
            {
                return NotFound($"No data found for entity {name}");
            }

            var item = GetType()
                .GetMethod("GetById")
                .MakeGenericMethod(entity.ToType())
                .Invoke(this, new object[] { id });

            if(item == null)
            {
                return NotFound();
            }

            return Ok(item);
        }

        public T GetById<T>(string id)
            where T : class
        {
            var repo = new EntityFrameworkRepository<T>(dbContext);
            return repo.GetById(id);
        }

        /// <summary>
        /// Delete a single entity item
        /// </summary>
        /// <param name="name">Data entity or type of document</param>
        /// <param name="id">Id</param>
        /// <returns></returns>
        [HttpDelete("{name}/data/{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public virtual async Task<IActionResult> DeleteData([FromRoute]string name, [FromRoute]string id)
        {
            var entity = await entitiesRepository.GetByIdAsync(name);
            if (entity == null)
            {
                return NotFound($"No data found for entity {name}");
            }

            var item = GetType()
                .GetMethod("GetById")
                .MakeGenericMethod(entity.ToType())
                .Invoke(this, new object[] { id });

            if (item == null)
            {
                return NotFound();
            }

            GetType()
                .GetMethod("Delete")
                .MakeGenericMethod(entity.ToType())
                .Invoke(this, new object[] { item });

            return NoContent();
        }

        public void Delete<T>(T item)
            where T : class
        {
            var repo = new EntityFrameworkRepository<T>(dbContext);
            repo.Delete(item);
        }
    }
}
