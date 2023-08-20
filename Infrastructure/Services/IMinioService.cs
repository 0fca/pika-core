using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Minio.DataModel;

namespace PikaCore.Infrastructure.Services
{

    public interface IMinioService : IDisposable
    {
        public Task<IList<Bucket>> GetBuckets();
        public Task<IList<Item>> ListObjects(string bucket, bool recursive = false, string? prefix = null);
        public Task<bool> StatObject(string bucket, string @object);
        public Task<ObjectStat?> ObjectInformation(string bucket, string @object);
        public Task<MemoryStream> GetObjectAsStream(string bucket, string @object, long offset = 1024);
        public Task PutObject(string fileName, Stream s, string bucket);
    }
}