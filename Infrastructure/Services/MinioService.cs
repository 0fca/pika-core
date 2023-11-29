using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Minio;
using Minio.DataModel;
using PikaCore.Infrastructure.Adapters.Minio;

namespace PikaCore.Infrastructure.Services
{

    public class MinioService : IMinioService
    {
        private readonly MinioClientBuilder _minioClientBuilder;
        private readonly MinioClient _minioClient;
        public MinioService(IConfiguration configuration)
        {
            _minioClientBuilder = MinioClientBuilder.Instance().CreateClientInstance();
            var endpoint = configuration.GetSection("Minio")["Endpoint"];
            var clientId = configuration.GetSection("Minio")["ClientId"];
            var password = configuration.GetSection("Minio")["Password"];
            var region = configuration.GetSection("Minio")["Region"];
            _minioClient = _minioClientBuilder.ConfigureClient(endpoint, clientId, password, region);
        }
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public async Task<IList<Bucket>> GetBuckets()
        {
            return (await _minioClient.ListBucketsAsync()).Buckets;
        }

        public async Task<IList<Item>> ListObjects(string bucket, bool recursive = false, string? prefix = null)
        {
            var items = new List<Item>(); 
            var args = new ListObjectsArgs()
                .WithBucket(bucket);
                if (!string.IsNullOrEmpty(prefix))
                {
                    args.WithPrefix(prefix);
                }
                args.WithRecursive(recursive);
                var lck = true;
                _minioClient.ListObjectsAsync(args).Subscribe(
                    item => items.Add(item),
                    ex =>
                    {
                        lck = false;
                        Console.WriteLine(ex.Message);
                    },
                    () =>  lck = false
                    );
                while (lck)
                {
                   Thread.Sleep(1); 
                }
                return items;
        }

        public async Task<bool> StatObject(string bucket, string @object)
        {
            try
            {
                var statObjectArgs = new StatObjectArgs()
                    .WithBucket(bucket)
                    .WithObject(@object);
                await _minioClient.StatObjectAsync(statObjectArgs);
            }
            catch(Exception ex)
            {
                return false;
            }

            return true;
        }

        public async Task<ObjectStat?> ObjectInformation(string bucket, string @object)
        {
            try
            {
                var statObjectArgs = new StatObjectArgs()
                    .WithBucket(bucket)
                    .WithObject(@object);
                return await _minioClient.StatObjectAsync(statObjectArgs);
            }
            catch(Exception ex)
            {
                return null;
            } 
        }

        public async Task<FileStream> GetObjectAsStream(string bucket, string @object, long offset = 0)
        {
            var fs = new FileStream(Path.Join(Path.GetTempPath(), Path.GetFileName(@object)), FileMode.Create);
            var getObjectArgs = new GetObjectArgs()
                .WithBucket(bucket)
                .WithObject(@object)
                .WithCallbackStream((stream) => stream.CopyTo(fs));
            await _minioClient.GetObjectAsync(getObjectArgs);
            return fs;
        }

        public async Task PutObject(string fileName, Stream s, string bucket)
        {
            try
            {
                var putObjectArgs = new PutObjectArgs()
                    .WithBucket(bucket)
                    .WithStreamData(s)
                    .WithObject(fileName)
                    .WithObjectSize(s.Length)
                    .WithContentType(MimeTypes.GetMimeType(fileName));
                await _minioClient.PutObjectAsync(putObjectArgs);
            }
            catch (Exception e)
            {
                throw new ApplicationException(e.Message);
            }
        }
    }
}