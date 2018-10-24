using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Community.OData.Linq;
using Community.OData.Linq.AspNetCore;
using DataHub.Entities;
using DataHub.Models.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RepositoryFramework.Interfaces;

namespace DataHub.Controllers
{
    [Route("files")]
#if RELEASE
    [Microsoft.AspNetCore.Authorization.Authorize]
#endif
    public class FilesController : Controller
    {
        private IQueryableRepository<Entities.FileInfo> filesRepository;
        private IBlobRepository blobRepository;

        public FilesController(
            IQueryableRepository<Entities.FileInfo> filesRepository,
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
        /// <param name="orderby">Order items by property values, see [OData Sorting](http://docs.oasis-open.org/odata/odata/v4.0/odata-v4.0-part1-protocol.html#_Toc445374629)</param>
        /// <param name="filter">Filter items by property values, see [OData Filtering](http://docs.oasis-open.org/odata/odata/v4.0/odata-v4.0-part1-protocol.html#_Toc445374625)</param>
        /// <returns></returns>
        [HttpGet()]
        [ProducesResponseType(typeof(FileListResponse), 200)]
        public async virtual Task<IActionResult> GetFilesAsync(
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
                var files = await Task.FromResult(filesRepository
                    .AsQueryable()
                    .OData()
                    .ApplyQueryOptionsWithoutSelectExpand(oDataQueryOptions)
                    .ToList());
                foreach (var file in files)
                {
                    file.DownloadUri = this.BuildLink($"files/{file.Id}");
                }

                return Ok(new FileListResponse
                {
                    Data = await PagedListData<FileInfo>.CreateAsync(files)
                });
            }
            catch (Exception e)
            {
                return this.InternalServerError(e.FlattenMessages());
            }
        }

        /// <summary>
        /// Get a file by id
        /// </summary>
        /// <param name="id">File id</param>
        /// <returns></returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(FileInfo), 200)]
        [ProducesResponseType(404)]
        public virtual async Task<IActionResult> GetFileAsync([FromRoute]string id)
        {
            try
            {
                var e = await filesRepository.GetByIdAsync(id);
                if (e == null)
                {
                    return NotFound($"No data found for file id {id}");
                }

                return Ok(e);
            }
            catch (Exception e)
            {
                return this.InternalServerError(e.FlattenMessages());
            }
        }

        /// <summary>
        /// Delete a file
        /// </summary>
        /// <param name="id">File id</param>
        /// <returns></returns>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(FileInfo), 204)]
        [ProducesResponseType(404)]
        public virtual async Task<IActionResult> DeleteFileAsync([FromRoute]string id)
        {
            try
            {
                var file = await filesRepository.GetByIdAsync(id);
                if (file == null)
                {
                    return NotFound($"No file with id {id}");
                }

                var blob = await blobRepository.GetByIdAsync(id);
                if (blob == null)
                {
                    return NotFound($"No payload found for file id {id}");
                }

                await blobRepository.DeleteAsync(blob);
                await filesRepository.DeleteAsync(file);

                return NoContent();
            }
            catch (Exception e)
            {
                return this.InternalServerError(e.FlattenMessages());
            }
        }

        /// <summary>
        /// Download file payload by id
        /// </summary>
        /// <param name="id">File id</param>
        /// <returns></returns>
        [HttpGet("{id}/payload")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public virtual async Task GetFilePayloadAsync([FromRoute]string id)
        {
            try
            {
                var file = await filesRepository.GetByIdAsync(id);
                if (file == null)
                {
                    Response.StatusCode = StatusCodes.Status404NotFound;
                    await Response.WriteAsync($"No file found with id {id}");
                    return;
                }

                var blob = await blobRepository.GetByIdAsync(id);
                if (blob == null)
                {
                    Response.StatusCode = StatusCodes.Status404NotFound;
                    await Response.WriteAsync($"No payload found for file id {id}");
                    return;
                }

                Response.Headers.Add("content-type", "application/octet-stream");
                var filename = string.IsNullOrWhiteSpace(file.Filename) ? id : file.Filename;
                Response.Headers.Add("content-disposition", $"attachment; filename={filename}");
                await blobRepository.DownloadAsync(blob, Response.Body);
            }
            catch (Exception e)
            {
                Response.StatusCode = StatusCodes.Status500InternalServerError;
                await Response.WriteAsync(string.Join('\n', e.FlattenMessages()));
            }
        }

        /// <summary>
        /// Upload a file
        /// </summary>
        /// <param name="source">Originating data source</param>
        /// <param name="assetId">Asset Id</param>
        /// <param name="filename">File name</param>
        /// <param name="format">File format</param>
        /// <param name="fileData">File payload</param>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(typeof(Entities.FileInfo), 201)]
        public virtual async Task<IActionResult> PostFileAsync(
            [FromForm]string source,
            [FromForm]string assetId,
            [FromForm]string filename,
            [FromForm]string format,
            [FromForm(Name = FileOperationFilter.FILE_PAYLOAD_PARM)] IFormFile fileData)
        {
            try
            {
                if (fileData == null)
                {
                    return BadRequest("No file data");
                }

                var id = Guid.NewGuid().ToString();
                var fileInfo = new Entities.FileInfo
                {
                    Id = id,
                    Source = source,
                    AssetId = assetId,
                    Filename = filename,
                    Format = format,
                    DownloadUri = this.BuildLink($"/files/{id}/payload")
                };

                await filesRepository.CreateAsync(fileInfo);

                using (var stream = fileData.OpenReadStream())
                {
                    await blobRepository.UploadAsync(new BlobInfo(id), stream);
                }

                return Created(this.BuildLink($"/files/{id}"), fileInfo);
            }
            catch (Exception e)
            {
                return this.InternalServerError(e.FlattenMessages());
            }
        }
    }
}
