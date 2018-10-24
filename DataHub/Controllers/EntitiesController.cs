using Community.OData.Linq;
using Community.OData.Linq.AspNetCore;
using DataHub.Entities;
using DataHub.Repositories;
using EFCore.BulkExtensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using RepositoryFramework.EntityFramework;
using RepositoryFramework.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DataHub.Controllers
{
    [Route("entities")]
#if RELEASE
    [Microsoft.AspNetCore.Authorization.Authorize]
#endif
    public class EntitiesController : Controller
    {
        private IEntitiesRepository entitiesRepository;
        private EntitiesDBContext dbContext;
        private IQueryableRepository<Entities.FileInfo> filesRepository;
        private IBlobRepository blobRepository;

        public EntitiesController(
            IEntitiesRepository entitiesRepository,
            EntitiesDBContext dbContext,
            IQueryableRepository<Entities.FileInfo> filesRepository,
            IBlobRepository blobRepository)
        {
            this.entitiesRepository = entitiesRepository;
            this.dbContext = dbContext;
            this.filesRepository = filesRepository;
            this.blobRepository = blobRepository;
        }

        /// <summary>
        /// Get list of entity schema information
        /// </summary>
        /// <param name="top">Show only the first n items, see [OData Paging - Top](http://docs.oasis-open.org/odata/odata/v4.0/odata-v4.0-part1-protocol.html#_Toc445374630)</param>
        /// <param name="skip">Skip the first n items, see [OData Paging - Skip](http://docs.oasis-open.org/odata/odata/v4.0/odata-v4.0-part1-protocol.html#_Toc445374631)</param>
        /// <param name="orderby">Order items by property values, see [OData Sorting](http://docs.oasis-open.org/odata/odata/v4.0/odata-v4.0-part1-protocol.html#_Toc445374629)</param>
        /// <param name="filter">Filter items by property values, see [OData Filtering](http://docs.oasis-open.org/odata/odata/v4.0/odata-v4.0-part1-protocol.html#_Toc445374625)</param>
        /// <returns></returns>
        [HttpGet()]
        [ProducesResponseType(typeof(EntityListResponse), 200)]
        public virtual async Task<IActionResult> GetEntitiesAsync(
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
                        async () => await Task.FromResult(entitiesRepository.AsQueryable().LongCount()))
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
                    //Select = select,
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

            return await Task.FromResult(repo
                .Include(queryOptions.Expand)
                .AsQueryable()
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
        /// <param name="source">Originating data source</param>
        /// <param name="id">Id</param>
        /// <returns></returns>
        [HttpGet("{name}/data/{source}/{id}")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(404)]
        public virtual async Task<IActionResult> GetDataByIdAsync(
            [FromRoute]string name,
            [FromRoute]string source,
            [FromRoute]string id)
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
                    .Invoke(this, new object[] { source, id });

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

        public async Task<object> GetByIdAsync<T>(string source, string id)
            where T : class, IEntity
        {
            var repo = new EntityFrameworkRepository<T>(dbContext);
            var findResult = await repo.FindAsync(e => e.Source == source && e.Id == id);
            return findResult.FirstOrDefault();
        }

        /// <summary>
        /// Update a single entity data item
        /// </summary>
        /// <param name="name">Data entity or type of document</param>
        /// <param name="source">Originating data source</param>
        /// <param name="id">Id</param>
        /// <param name="item">Data item to update</param>
        /// <returns></returns>
        [HttpPut("{name}/data/{source}/{id}")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(404)]
        public virtual async Task<IActionResult> UpdateDataAsync(
            [FromRoute]string name,
            [FromRoute]string source,
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
                    .Invoke(this, new object[] { source, id, item }));
            }
            catch (KeyNotFoundException e)
            {
                return NotFound(e.Message);
            }
            catch (Exception e)
            {
                return this.InternalServerError(e.FlattenMessages());
            }
        }

        public async Task<object> UpdateAsync<T>(string source, string id, dynamic item)
            where T : class, IEntity
        {
            var repo = new EntityFrameworkRepository<T>(dbContext);
            var existing = await repo.FindAsync(e => e.Source == source && e.Id == id);
            if (existing == null || existing.Count() == 0)
            {
                throw new KeyNotFoundException($"Entity {typeof(T)} with source {source} and id {id} not found");
            }

            await repo.DeleteAsync(existing.First());
            var inItem = JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(item));
            await repo.CreateAsync(inItem);
            return inItem;
        }

        /// <summary>
        /// Delete a single entity item
        /// </summary>
        /// <param name="name">Data entity or type of document</param>
        /// <param name="source">Originating data source</param>
        /// <param name="id">Id</param>
        /// <returns></returns>
        [HttpDelete("{name}/data/{source}/{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public virtual async Task<IActionResult> DeleteDataAsync(
            [FromRoute]string name,
            [FromRoute]string source,
            [FromRoute]string id)
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
                    .Invoke(this, new object[] { source, id });

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

        /// <summary>
        /// Upload a file
        /// </summary>
        /// <param name="name">Data entity or type of document</param>
        /// <param name="fileData">File payload</param>
        /// <param name="separator">Column separator character. Default is tab</param>
        /// <returns></returns>
        [HttpPost("{name}/data/csv")]
        [ProducesResponseType(typeof(Entities.FileInfo), 201)]
        public virtual async Task<IActionResult> PostCsvFileAsync(
            [FromRoute]string name,
            [FromForm(Name = FileOperationFilter.FILE_PAYLOAD_PARM)] IFormFile fileData,
            [FromForm]char separator = '\t')
        {
            try
            {
                var entity = entitiesRepository.GetById(name);
                if (entity == null)
                {
                    return NotFound($"Unknown entity {name}");
                }

                using (var stream = fileData.OpenReadStream())
                {
                    await (Task)GetType()
                    .GetMethod("OnFileUploadAsync")
                    .MakeGenericMethod(entity.ToType())
                    .Invoke(this, new object[] { stream, separator });
                }

                return Created(this.BuildLink($"{name}/data/csv"), name);
            }
            catch (Exception e)
            {
                return this.InternalServerError(e.FlattenMessages());
            }
        }

        public async Task OnFileUploadAsync<T>(Stream stream, char separator)
            where T : class, IEntity, new()
        {
            StreamReader sr = new StreamReader(stream);
            var headers = new List<string>(sr.ReadLine().Split(separator).Select(s => s.ToUpper()));
            var props = typeof(T).GetProperties();
            var columnsHash = new Hashtable();
            var entities = new List<T>();
            foreach (var prop in props)
            {
                var i = headers.IndexOf(prop.Name.ToUpper());
                if (i >= 0)
                {
                    columnsHash.Add(prop.Name, i);
                }
            }

            while (!sr.EndOfStream)
            {
                string[] values = Regex.Split(sr.ReadLine(), $"{separator}(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");
                T entity = new T();
                foreach (var prop in props)
                {
                    if (columnsHash.ContainsKey(prop.Name))
                    {
                        var text = values[(int)columnsHash[prop.Name]];
                        TypeConverter tc = TypeDescriptor.GetConverter(prop.PropertyType);
                        var value = string.IsNullOrWhiteSpace(text) 
                            ? null 
                            : tc.ConvertFromString(null, CultureInfo.InvariantCulture, text);
                        prop.SetValue(entity, value);
                    }
                }

                var complexEntity = entity as IComplexEntity;
                if(complexEntity != null && complexEntity.ParentId != null)
                {
                    complexEntity.ParentSource = complexEntity.Source;
                }
                entities.Add(entity);
            }

            var sql = $"ALTER TABLE {typeof(T).Name} NOCHECK CONSTRAINT ALL; SELECT TOP 1 * FROM {typeof(T).Name}";
            dbContext.Set<T>().FromSql(sql).ToList();

            await dbContext.BulkInsertOrUpdateAsync<T>(entities);

            sql = $"ALTER TABLE {typeof(T).Name} WITH CHECK CHECK CONSTRAINT ALL; SELECT TOP 1 * FROM {typeof(T).Name}";
            dbContext.Set<T>().FromSql(sql).ToList();
        }
    }
}