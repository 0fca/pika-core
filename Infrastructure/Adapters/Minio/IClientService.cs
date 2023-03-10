using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Minio.DataModel;

namespace PikaCore.Infrastructure.Adapters.Minio
{

    public interface IClientService : IDisposable
    {
        public Task<IList<Bucket>> GetBuckets();
        public Task<IList<Item>> ListObjects(string bucket, bool recursive = false, string? prefix = null);
    }
}