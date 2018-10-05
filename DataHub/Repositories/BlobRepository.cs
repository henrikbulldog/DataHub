using RepositoryFramework.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DataHub.Repositories
{
    public class BlobRepository : IBlobRepository
    {
        public void Delete(BlobInfo entity)
        {
            throw new NotImplementedException();
        }

        public Task DeleteAsync(BlobInfo entity)
        {
            throw new NotImplementedException();
        }

        public void DeleteMany(IEnumerable<BlobInfo> entities)
        {
            throw new NotImplementedException();
        }

        public Task DeleteManyAsync(IEnumerable<BlobInfo> entities)
        {
            throw new NotImplementedException();
        }

        public void Download(BlobInfo entity, Stream stream)
        {
            string s = "abcABC123";
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
            throw new NotImplementedException();
        }

        public IEnumerable<BlobInfo> Find(string filter)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<BlobInfo>> FindAsync()
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<BlobInfo>> FindAsync(string filter)
        {
            throw new NotImplementedException();
        }

        public BlobInfo GetById(object id)
        {
            if (id.ToString() != "1")
                return null;

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
