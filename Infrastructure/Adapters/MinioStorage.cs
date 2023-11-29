using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Minio.DataModel;
using Pika.Domain.Storage.Entity.View;
using PikaCore.Areas.Core.Models.File;
using PikaCore.Areas.Core.Queries;
using PikaCore.Infrastructure.Services;

namespace PikaCore.Infrastructure.Adapters;

public class MinioStorage : IStorage
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;
    private readonly IDistributedCache _cache;
    private readonly IMinioService _minioService;

    public MinioStorage(IMediator mediator,
        IMapper mapper,
        IDistributedCache cache,
        IMinioService minioService)
    {
        _cache = cache;
        _mediator = mediator;
        _mapper = mapper;
        _minioService = minioService;
    }

    public async Task<List<BucketsView>> GetBucketsForRole(string roleString)
    {
        var buckets = await _mediator.Send(new GetBucketsForRoleQuery { RoleString = roleString });
        return new List<BucketsView>(buckets);
    }

    public async Task<List<CategoriesView>> GetCategoriesForBucket(Guid bucketId)
    {
        var bucketsCategoriesMaps = await _cache.GetStringAsync("buckets.categories.map");
        var bucketsToCategories = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(
            bucketsCategoriesMaps ?? "{}"
        );
        var categoriesViews = new List<CategoriesView>();
        if (!bucketsToCategories!.ContainsKey(bucketId.ToString()))
        {
            return categoriesViews;
        }

        var categoriesIds = bucketsToCategories![bucketId.ToString()];
        foreach (var id in categoriesIds)
        {
            categoriesViews.Add(_mapper.Map<CategoriesView>(
                    await _mediator.Send(new GetCategoryByIdQuery(Guid.Parse(id)))
                )
            );
        }

        return categoriesViews;
    }

    public async Task<bool> StatObject(string bucket, string @object)
    {
        return await _minioService.StatObject(bucket, @object);
    }

    public async Task<ObjectInfo?> ObjectInformation(string bucketName, string objectName)
    {
        var statObject = await _minioService.ObjectInformation(bucketName, objectName);
        return statObject == null ? null : _mapper.Map<ObjectStat, ObjectInfo>(statObject);
    }

    public async Task<Tuple<FileStream, string, string>> GetObjectAsStream(string bucket,
        string @object,
        long offset = 1024)
    {
        var returnStream = await _minioService.GetObjectAsStream(bucket, @object, offset);
        returnStream.Position = 0;
        return new Tuple<FileStream, string, string>(returnStream,
            Path.GetFileName(@object),
            MimeTypes.GetMimeType(@object));
    }

    public async Task<bool> UserHasBucketAccess(Guid bucketId, ClaimsPrincipal user)
    {
        var bucket = await _mediator.Send(new GetBucketByIdQuery(bucketId));
        if (bucket.RoleClaims == null)
        {
            throw new ApplicationException(
                $"There was an error during downloading claims for bucket of {bucket.Id}"
            );
        }

        return user.Claims.Any(c => bucket.RoleClaims!.Contains(c.Value));
    }

    public void Dispose()
    {
        _minioService.Dispose();
        GC.SuppressFinalize(this);
    }
}