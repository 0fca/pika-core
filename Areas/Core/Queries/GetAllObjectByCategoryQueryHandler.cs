using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using PikaCore.Areas.Core.Models.File;

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
            var hostName = Environment.GetEnvironmentVariable("HOSTNAME");
            RecurringJob.TriggerJob($"{hostName}-UpdateCategories");
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