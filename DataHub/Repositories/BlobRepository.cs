using RepositoryFramework.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace DataHub.Repositories
{
    public class BlobRepository : IBlobRepository
    {
        public void Delete(BlobInfo entity)
        {
        }

        public Task DeleteAsync(BlobInfo entity)
        {
            return Task.Run(() => { });
        }

        public void DeleteMany(IEnumerable<BlobInfo> entities)
        {
        }

        public Task DeleteManyAsync(IEnumerable<BlobInfo> entities)
        {
            return Task.Run(() => { });
        }

        public void Download(BlobInfo entity, Stream stream)
        {
            string s = "From Mock BLOB repository";
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
        }

        public async Task DownloadAsync(BlobInfo entity, Stream stream)
        {
            await Task.Run(() => Download(entity, stream));
        }

        public IEnumerable<BlobInfo> Find()
        {
            return new List<BlobInfo>
            {
                new BlobInfo
                {
                    Id = Guid.NewGuid().ToString()
                }
            };
        }

        public IEnumerable<BlobInfo> Find(string filter)
        {
            return Find();
        }

        public Task<IEnumerable<BlobInfo>> FindAsync()
        {
            return Task.Run(() => Find());
        }

        public Task<IEnumerable<BlobInfo>> FindAsync(string filter)
        {
            return Task.Run(() => Find());
        }

        public BlobInfo GetById(object id)
        {
            return new BlobInfo
            {
                Id = id.ToString()
            };
        }

        public async Task<BlobInfo> GetByIdAsync(object id)
        {
            return await Task.Run(() => GetById(id));
        }

        public void Upload(BlobInfo entity, Stream stream)
        {
        }

        public async Task UploadAsync(BlobInfo entity, Stream stream)
        {
            await Task.Run(() => { });
        }
    }
}
