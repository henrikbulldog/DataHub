using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Community.OData.Linq;
using Community.OData.Linq.AspNetCore;
using DataHub.Entities;
using DataHub.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using RepositoryFramework.EntityFramework;
using System;

namespace DataHub.Controllers
{
    [Route("entities")]
    public class EntitiesController : Controller
    {
        private IEntitiesRepository entitiesRepository;
        private EntitiesDBContext dbContext;

        public EntitiesController(
            IEntitiesRepository entitiesRepository,
            EntitiesDBContext dbContext)
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
        public virtual async Task<IActionResult> GetEntitiesAsync(
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

                var entities = await Task.FromResult(entitiesRepository
                    .AsQueryable()
                    .OData()
                    .ApplyQueryOptionsWithoutSelectExpand(oDataQueryOptions)
                    .ToList());

                return Ok(new EntityListResponse
                {
                    SchemaVersion = Assembly.GetAssembly(typeof(DataHub.Entities.Asset)).GetName().Version.ToString(),
                    Data = await PagedListData<Entity>.CreateAsync(
                        entities, 
                        top, 
                        skip, 
                        async () => await entitiesRepository.AsQueryable().LongCountAsync())
                });
            }
            catch (Exception e)
            {
                return this.InternalServerError(e.FlattenMessages());
            }
        }

        /// <summary>
        /// Get schema information of a single entity
        /// </summary>
        /// <param name="name">Data entity or type of document</param>
        /// <returns></returns>
        [HttpGet("{name}")]
        [ProducesResponseType(typeof(Entity), 200)]
        [ProducesResponseType(404)]
        public virtual async Task<IActionResult> GetEntityByNameAsync([FromRoute]string name)
        {
            try
            {
                var entity = await entitiesRepository.GetByIdAsync(name);
                if (entity == null)
                {
                    return NotFound($"No data found for entity {name}");
                }

                return Ok(entity);
            }
            catch (Exception e)
            {
                return this.InternalServerError(e.FlattenMessages());
            }
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
        public async virtual Task<IActionResult> GetDataAsync(
            [FromRoute]string name,
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

                var entity = entitiesRepository.GetById(name);
                if (entity == null)
                {
                    return NotFound($"No data found for entity {name}");
                }

                var items = await (Task<IEnumerable<object>>)GetType()
                    .GetMethod("ApplyQueryOptionsAsync")
                    .MakeGenericMethod(entity.ToType())
                    .Invoke(this, new object[] { oDataQueryOptions });

                return Ok(new DataListResponse
                {
                    Data = await PagedListData<object>.CreateAsync(
                        items, 
                        top, 
                        skip, 
                        async () => await (Task<long?>)GetType()
                            .GetMethod("CountAsync")
                            .MakeGenericMethod(entity.ToType())
                            .Invoke(this, null))
                });
            }
            catch (Exception e)
            {
                return this.InternalServerError(e.FlattenMessages());
            }
        }

        public async Task<IEnumerable<object>> ApplyQueryOptionsAsync<T>(ODataQueryOptions queryOptions)
            where T : class
        {
            var repo = new EntityFrameworkRepository<T>(dbContext);
            return await Task.FromResult(repo.AsQueryable()
                .OData()
                .ApplyQueryOptionsWithoutSelectExpand(queryOptions)
                .ToList());
        }

        public async Task<long?> CountAsync<T>()
            where T : class
        {
            var repo = new EntityFrameworkRepository<T>(dbContext);
            return await repo.AsQueryable()
                .LongCountAsync();
        }

        /// <summary>
        /// Create entity data items
        /// </summary>
        /// <param name="name">Data entity or type of document</param>
        /// <param name="items">List of entity data items</param>
        /// <returns></returns>
        [HttpPost("{name}/data")]
        [ProducesResponseType(typeof(object), 201)]
        [ProducesResponseType(404)]
        public async virtual Task<IActionResult> PostDataAsync(
            [FromRoute]string name,
            [FromBody]List<dynamic> items)
        {
            try
            {
                var entity = entitiesRepository.GetById(name);
                if (entity == null)
                {
                    return NotFound($"Unknown entity {name}");
                }

                var newItems = await (Task<IEnumerable<object>>)GetType()
                    .GetMethod("CreateManyAsync")
                    .MakeGenericMethod(entity.ToType())
                    .Invoke(this, new object[] { items });

                return Created(this.BuildLink($"/entities/{name}"), newItems);
            }
            catch (Exception e)
            {
                return this.InternalServerError(e.FlattenMessages());
            }
        }

        public async Task<IEnumerable<object>> CreateManyAsync<T>(List<dynamic> items)
            where T : class, new()
        {
            var repo = new EntityFrameworkRepository<T>(dbContext);
            var newItems = JsonConvert.DeserializeObject<List<T>>(
                JsonConvert.SerializeObject(items));

            await repo.CreateManyAsync(newItems);
            return newItems;
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
        public virtual async Task<IActionResult> GetDataByIdAsync([FromRoute]string name, [FromRoute]string id)
        {
            try
            {
                var entity = await entitiesRepository.GetByIdAsync(name);
                if (entity == null)
                {
                    return NotFound($"No data found for entity {name}");
                }

                var item = await (Task<object>)GetType()
                    .GetMethod("GetByIdAsync")
                    .MakeGenericMethod(entity.ToType())
                    .Invoke(this, new object[] { id });

                if (item == null)
                {
                    return NotFound();
                }

                return Ok(item);
            }
            catch (Exception e)
            {
                return this.InternalServerError(e.FlattenMessages());
            }
        }

        public async Task<object> GetByIdAsync<T>(string id)
            where T : class
        {
            var repo = new EntityFrameworkRepository<T>(dbContext);
            return await repo.GetByIdAsync(id);
        }

        /// <summary>
        /// Update a single entity data item
        /// </summary>
        /// <param name="name">Data entity or type of document</param>
        /// <param name="id">Id</param>
        /// <param name="item">Data item to update</param>
        /// <returns></returns>
        [HttpPut("{name}/data/{id}")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(404)]
        public virtual async Task<IActionResult> UpdateDataAsync(
            [FromRoute]string name, 
            [FromRoute]string id, 
            [FromBody]dynamic item)
        {
            try
            {
                var entity = await entitiesRepository.GetByIdAsync(name);
                if (entity == null)
                {
                    return NotFound($"No data found for entity {name}");
                }

                return Ok(await (Task<object>)GetType()
                    .GetMethod("UpdateAsync")
                    .MakeGenericMethod(entity.ToType())
                    .Invoke(this, new object[] { id, item }));
            }
            catch(KeyNotFoundException e)
            {
                return NotFound(e.Message);
            }
            catch (Exception e)
            {
                return this.InternalServerError(e.FlattenMessages());
            }
        }

        public async Task<object> UpdateAsync<T>(string id, dynamic item)
            where T : class
        {
            var repo = new EntityFrameworkRepository<T>(dbContext);
            var existing = await repo.GetByIdAsync(id);
            if(existing == null)
            {
                throw new KeyNotFoundException($"Entity {typeof(T)} with id {id} not found");
            }

            await repo.DeleteAsync(existing);
            var inItem = JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(item));
            await repo.CreateAsync(inItem);
            return inItem;
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
        public virtual async Task<IActionResult> DeleteDataAsync([FromRoute]string name, [FromRoute]string id)
        {
            try
            {
                var entity = await entitiesRepository.GetByIdAsync(name);
                if (entity == null)
                {
                    return NotFound($"No data found for entity {name}");
                }

                var item = await (Task<object>)GetType()
                    .GetMethod("GetByIdAsync")
                    .MakeGenericMethod(entity.ToType())
                    .Invoke(this, new object[] { id });

                if (item == null)
                {
                    return NotFound();
                }

                await (Task)GetType()
                    .GetMethod("DeleteAsync")
                    .MakeGenericMethod(entity.ToType())
                    .Invoke(this, new object[] { item });

                return NoContent();
            }
            catch (Exception e)
            {
                return this.InternalServerError(e.FlattenMessages());
            }
        }

        public async Task DeleteAsync<T>(T item)
            where T : class
        {
            var repo = new EntityFrameworkRepository<T>(dbContext);
            await repo.DeleteAsync(item);
        }
    }
}
