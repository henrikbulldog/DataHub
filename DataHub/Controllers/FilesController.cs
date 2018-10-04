using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Community.OData.Linq;
using Community.OData.Linq.AspNetCore;
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

        [HttpGet()]
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
            foreach(var file in files)
            {
                file.DownloadUri = this.BuildLink($"files/{file.Id}");
            }
            return Ok(files);
        }

        [HttpGet("{id}")]
        public virtual async Task<IActionResult> GetFile([FromRoute]string id)
        {
            var e = await filesRepository.GetByIdAsync(id);
            if (e == null)
            {
                return NotFound($"No data found for file id {id}");
            }

            return Ok(e);
        }

        [HttpGet("{id}/payload")]
        public virtual async Task GetFilePayload([FromRoute]string id)
        {
            var blob = await blobRepository.GetByIdAsync(id);
            if (blob == null)
            {
                await this.WriteNotFound($"No payload found for file id {id}");
                return;
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
