using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.Common;
using JasperFx.Core;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Pika.Domain.Storage.Callables;
using Pika.Domain.Storage.Callables.ValueTypes;
using PikaCore.Areas.Core.Queries;
using PikaCore.Infrastructure.Adapters.Minio;
using PikaCore.Infrastructure.Services;

namespace PikaCore.Areas.Core.Callables;

public class GenerateCategoriesTagsCallable : BaseJobCallable
{
    private readonly IMediator _mediator;
    private readonly IMinioService _minioService;
    private readonly IDistributedCache _distributedCache;

    public GenerateCategoriesTagsCallable(IMediator mediator,
        IMinioService minioService,
        IDistributedCache cache)
    {
        this._mediator = mediator;
        this._minioService = minioService;
        this._distributedCache = cache;
    }

    public override async Task Execute(Dictionary<string, ParameterValueType>? parameterValueTypes)
    {
        var categories = await _mediator.Send(new GetAllCategoriesQuery());
        var buckets = await _mediator.Send(new GetAllBucketsQuery());

        var tagsCategoriesMap = new Dictionary<Guid, Dictionary<Guid, HashSet<string>>>();
        var mimeTypes = new Winista.Mime.MimeTypes();
        foreach (var bucket in buckets)
        {
            var items = _minioService
                .ListObjects(bucket.Name, true).Result;

            foreach (var c in categories)
            {
                foreach (var i in items)
                {
                    var mime = mimeTypes.GetMimeType(i.Key);
                    if (mime is null || !c.Mimes.Contains(mime.Name)) continue;
                    var tag = new List<string>();
                    if (i.Key.Contains('/'))
                    {
                        var s = new Stack<string>(i.Key.Split('/'));
                        s.Pop();
                        tag.AddRange(s);
                    }
                    if (!tagsCategoriesMap.ContainsKey(c.Id))
                    {
                        tagsCategoriesMap.Add(c.Id, new Dictionary<Guid, HashSet<string>>());
                    }

                    if (!tagsCategoriesMap[c.Id].ContainsKey(bucket.Id))
                    {
                        tagsCategoriesMap[c.Id].Add(bucket.Id, new HashSet<string>());
                    }

                    tag.ForEach(t =>
                    {
                        if (!string.IsNullOrEmpty(t))
                        {
                            tagsCategoriesMap[c.Id][bucket.Id].Add(t);
                        }
                    });
                }
            }
        }

        var parameterDictList = new List<Dictionary<string, ParameterValueType>>();
        foreach (var (categoryId, tags) in tagsCategoriesMap)
        {
            var parameters = new Dictionary<string, ParameterValueType>
            {
                ["Id"] = new(categoryId),
                ["Name"] = new(""),
                ["Mimes"] = new(new List<string>()),
                ["Tags"] = new(tags)
            };
            parameterDictList.Add(parameters);
        }

        var serializedDict = JsonConvert.SerializeObject(parameterDictList); 
        await _distributedCache.SetStringAsync("update.categories.parameters",
            serializedDict,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(300)
            }
        );
        var updateCallable = new UpdateCategoryCallable(_mediator, _distributedCache);
        var jobId = BackgroundJob.Schedule("default", 
            () => updateCallable.Execute(null), 
            TimeSpan.FromMilliseconds(500));
        await _distributedCache.SetStringAsync("update.job.identifier", jobId, 
            new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(300)
        });
    }
}