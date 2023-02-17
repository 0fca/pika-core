using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Minio.DataModel;

namespace PikaCore.Infrastructure.Adapters.Minio
{

    public class ClientService : IClientService
    {
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public void Init(string endpoint, string clientId, string password)
        {
            throw new NotImplementedException();
        }

        public void ConfigureBucket(string bucketName)
        {
            throw new NotImplementedException();
        }

        public Task<IList<Bucket>> GetBuckets()
        {
            throw new NotImplementedException();
        }

        public Task<IObservable<Item>> ListObjects()
        {
            throw new NotImplementedException();
        }

        public void ConfigureBucket()
        {

        }
    }
}