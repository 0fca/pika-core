using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using Pika.Domain.Storage.Entity.View;
using PikaCore.Areas.Core.Models.File;

namespace PikaCore.Infrastructure.Adapters;

public interface IStorage : IDisposable
{
    public Task<List<BucketsView>> GetBucketsForRole(string roleString);
    public Task<List<CategoriesView>> GetCategoriesForBucket(Guid id);
    public Task<bool> UserHasBucketAccess(Guid bucketId, ClaimsPrincipal user);

    public Task<bool> StatObject(string bucketName, string @object);

    public Task<ObjectInfo?> ObjectInformation(string bucketName, string @objectName);
    public Task<Tuple<FileStream, string, string>> GetObjectAsStream(string bucket, string @object, long offset = 1024L);
}