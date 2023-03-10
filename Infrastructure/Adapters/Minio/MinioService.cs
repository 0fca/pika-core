using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Minio;
using Minio.DataModel;

namespace PikaCore.Infrastructure.Adapters.Minio
{

    public class MinioService : IClientService
    {
        private readonly MinioClientBuilder _minioClientBuilder;
        private readonly IConfiguration _configuration;
        private readonly MinioClient _minioClient;
        public MinioService(IConfiguration configuration)
        {
            _minioClientBuilder = MinioClientBuilder.Instance().CreateClientInstance();
            var endpoint = configuration.GetSection("Minio")["Endpoint"];
            var clientId = configuration.GetSection("Minio")["ClientId"];
            var password = configuration.GetSection("Minio")["Password"];
            _minioClient = _minioClientBuilder.ConfigureClient(endpoint, clientId, password);
        }
        public void Dispose()
        {
            _minioClient.Dispose();
            GC.SuppressFinalize(this);
        }

        public async Task<IList<Bucket>> GetBuckets()
        {
            return (await _minioClient.ListBucketsAsync()).Buckets;
        }

        public async Task<IList<Item>> ListObjects(string bucket, bool recursive = false, string? prefix = null)
        {
            var items = new List<Item>(); 
            ListObjectsArgs args = new ListObjectsArgs()
                .WithBucket(bucket);
                if (!string.IsNullOrEmpty(prefix))
                {
                    args.WithPrefix(prefix);
                }
                args.WithRecursive(recursive);
                var lck = true;
                _minioClient.ListObjectsAsync(args).Subscribe(
                    item => items.Add(item),
                    ex => lck = false,
                    () =>  lck = false
                    );
                while (lck)
                {
                   Thread.Sleep(1); 
                }
                return items;
        }
    }
}