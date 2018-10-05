using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Community.OData.Linq;
using Community.OData.Linq.AspNetCore;
using DataHub.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RepositoryFramework.Interfaces;

namespace DataHub.Controllers
{
    [Route("files")]
    public class FilesController : Controller
    {
        private IQueryableRepository<Models.FileInfo> filesRepository;
        private IBlobRepository blobRepository;

        public FilesController(
            IQueryableRepository<Models.FileInfo> filesRepository,
            IBlobRepository blobRepository)
        {
            this.filesRepository = filesRepository;
            this.blobRepository = blobRepository;
        }

        /// <summary>
        /// Get a list of files
        /// </summary>
        /// <param name="top">Show only the first n items, see [OData Paging - Top](http://docs.oasis-open.org/odata/odata/v4.0/odata-v4.0-part1-protocol.html#_Toc445374630)</param>
        /// <param name="skip">Skip the first n items, see [OData Paging - Skip](http://docs.oasis-open.org/odata/odata/v4.0/odata-v4.0-part1-protocol.html#_Toc445374631)</param>
        /// <param name="select"> Select properties to be returned, see [OData Select](http://docs.oasis-open.org/odata/odata/v4.0/odata-v4.0-part1-protocol.html#_Toc445374620)</param>
        /// <param name="orderby">Order items by property values, see [OData Sorting](http://docs.oasis-open.org/odata/odata/v4.0/odata-v4.0-part1-protocol.html#_Toc445374629)</param>
        /// <param name="expand">Expand object and collection properties, see [OData Expand](http://docs.oasis-open.org/odata/odata/v4.0/errata03/os/complete/part1-protocol/odata-v4.0-errata03-os-part1-protocol-complete.html#_System_Query_Option_6)</param>
        /// <param name="filter">Filter items by property values, see [OData Filtering](http://docs.oasis-open.org/odata/odata/v4.0/odata-v4.0-part1-protocol.html#_Toc445374625)</param>
        /// <returns></returns>
        [HttpGet()]
        [ProducesResponseType(typeof(FileListResponse), 200)]
        public virtual IActionResult GetFiles(
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
            var files = filesRepository.AsQueryable()
                .OData().ApplyQueryOptionsWithoutSelectExpand(oDataQueryOptions);
            foreach (var file in files)
            {
                file.DownloadUri = this.BuildLink($"files/{file.Id}");
            }

            return Ok(new FileListResponse
            {
                Data = new PagedListData<FileInfo>(files)
            });
        }

        /// <summary>
        /// Get a file by id
        /// </summary>
        /// <param name="id">File id</param>
        /// <returns></returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(FileInfo), 200)]
        [ProducesResponseType(404)]
        public virtual async Task<IActionResult> GetFile([FromRoute]string id)
        {
            var e = await filesRepository.GetByIdAsync(id);
            if (e == null)
            {
                return NotFound($"No data found for file id {id}");
            }

            return Ok(e);
        }

        /// <summary>
        /// Download file payload by id
        /// </summary>
        /// <param name="id">File id</param>
        /// <returns></returns>
        [HttpGet("{id}/payload")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public virtual async Task GetFilePayload([FromRoute]string id)
        {
            var blob = await blobRepository.GetByIdAsync(id);
            if (blob == null)
            {
                await this.WriteNotFound($"No payload found for file id {id}");
                NotFound();
            }

            Response.Headers.Add("content-type", "application/octet-stream");
            Response.Headers.Add("content-disposition", $"attachment; filename={id}");
            await blobRepository.DownloadAsync(blob, Response.Body);
        }

        /// <summary>
        /// Upload a file
        /// </summary>
        /// <param name="source">Originating data source</param>
        /// <param name="entity">Data entity or type of document</param>
        /// <param name="filename">File name</param>
        /// <param name="format">File format</param>
        /// <param name="fileData">File payload</param>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(typeof(Models.FileInfo), 201)]
        public virtual async Task<IActionResult> PostFile(
            [FromForm]string source,
            [FromForm]string entity,
            [FromForm]string filename,
            [FromForm]string format,
            [FromForm]IFormFile fileData)
        {
            if (fileData == null)
            {
                return BadRequest("No file data");
            }

            var id = Guid.NewGuid().ToString();
            var fileInfo = new Models.FileInfo
            {
                Id = id,
                Source = source,
                Entity = entity,
                Filename = filename,
                Format = format,
                DownloadUri = this.BuildLink($"/files/{id}/payload")
            };

            filesRepository.Create(fileInfo);
            
            using (var stream = fileData.OpenReadStream())
            {
                await blobRepository.UploadAsync(new BlobInfo(id), stream);
            }

            return Created(this.BuildLink($"/files/{id}"), fileInfo);
        }
    }
}
