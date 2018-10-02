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
    [Route("[controller]")]
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
                Filters = new List<string> { filter }
            };
            var files = filesRepository.AsQueryable()
                .OData().ApplyQueryOptionsWithoutSelectExpand(oDataQueryOptions);
            foreach(var file in files)
            {
                file.Uri = this.BuildLink($"files/{file.Id}");
            }
            return Ok(files);
        }

        [HttpGet("{id}")]
        public virtual async Task GetById([FromRoute]string id)
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

        [HttpPost]
        public virtual async Task<IActionResult> Post(
            [FromHeader]string source, 
            [FromHeader]string entity, 
            [FromHeader]string filename, 
            [FromHeader]string format, 
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
                Uri = this.BuildLink($"/files/{id}")
            };
            using (var stream = fileData.OpenReadStream())
            {
                await blobRepository.UploadAsync(new BlobInfo(id), stream);
            }
            return Ok();
        }
    }
}
