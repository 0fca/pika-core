using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Minio.DataModel;

namespace PikaCore.Infrastructure.Adapters.Minio
{

    public interface IClientService : IDisposable
    {
        public void Init(string endpoint, string clientId, string password);
        public void ConfigureBucket(string bucketName);
        public Task<IList<Bucket>> GetBuckets();
        public Task<IObservable<Item>> ListObjects();
    }
}