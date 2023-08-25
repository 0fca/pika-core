using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Hangfire;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Pika.Domain.Storage.Repository;
using PikaCore.Areas.Core.Models.File;
using PikaCore.Infrastructure.Adapters.Minio;
using PikaCore.Infrastructure.Services;

namespace PikaCore.Areas.Core.Queries;

public class GetAllObjectByCategoryQueryHandler : IRequestHandler<GetAllObjectsByCategoryQuery, List<ObjectInfo>>
{

    private readonly IDistributedCache _distributedCache;
    public GetAllObjectByCategoryQueryHandler(IDistributedCache distributedCache) 
    {
        this._distributedCache = distributedCache;
    }
    public async Task<List<ObjectInfo>> Handle(GetAllObjectsByCategoryQuery request, CancellationToken cancellationToken)
    {
        var serializedObjectInfos = await _distributedCache.GetStringAsync(
            $"category.contents.{request.CategoryId()}",
            cancellationToken);
        if (string.IsNullOrEmpty(serializedObjectInfos))
        {
            RecurringJob.TriggerJob("UpdateCategories");
            return new List<ObjectInfo>(); //TODO: Just for now, need to handle it properly
        }
        var objectInfos = JsonSerializer
            .Deserialize<List<ObjectInfo>>(json: serializedObjectInfos);
        if (objectInfos == null)
        {
            throw new InvalidOperationException("Cannot load objects");
        }
        return objectInfos;
    }
}