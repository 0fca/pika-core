using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Minio.DataModel;
using Newtonsoft.Json;
using NuGet.Packaging;
using Pika.Domain.Storage.Callables;
using Pika.Domain.Storage.Callables.ValueTypes;
using Pika.Domain.Storage.Entity.View;
using PikaCore.Areas.Core.Queries;
using PikaCore.Infrastructure.Services;
using Serilog;
using Guid = System.Guid;

namespace PikaCore.Areas.Core.Callables;

public class GenerateCategoriesTagsCallable : BaseJobCallable
{
    private readonly IMediator _mediator;
    private readonly IMinioService _minioService;
    private readonly IDistributedCache _distributedCache;

    private static readonly Dictionary<Guid, Dictionary<Guid, HashSet<string>>> CategoriesTagsMap = new();
    
    public GenerateCategoriesTagsCallable(IMediator mediator,
        IMinioService minioService,
        IDistributedCache cache) : base(cache)
    {
        this._mediator = mediator;
        this._minioService = minioService;
        this._distributedCache = cache;
    }

    public override async Task Execute(Dictionary<string, ParameterValueType>? parameterValueTypes)
    {
        var buckets = await _mediator.Send(new GetAllBucketsQuery());

        var categories = await _mediator.Send(new GetAllCategoriesQuery());
        
        foreach (var bucket in buckets)
        {
            var items = _minioService
                .ListObjects(bucket.Name, true);
            items.Subscribe(
                item => OnNext(item, bucket, categories),
                OnError, 
                OnCompleted
            );
        }
        var updateCallable = new UpdateCategoryCallable(_mediator, _distributedCache);
        var hostName = Environment.GetEnvironmentVariable("HOSTNAME");
        var jobId = BackgroundJob.Schedule(hostName.ToLower(),
            () => updateCallable.Execute(null),
            TimeSpan.FromSeconds(5));
        await _distributedCache.SetStringAsync("update.job.identifier", jobId,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(300)
            });
        BackgroundJob.Requeue(jobId);
    }

    private void OnNext(Item i, BucketsView bucket, 
        IEnumerable<CategoriesView> categories)
    {
 
        var mime = MimeTypes.GetMimeType(i.Key);
        if (string.IsNullOrEmpty(mime))
        {
            return;
        }
        var categoriesView = categories
            .FirstOrDefault(c => c.Mimes.Contains(mime));
        
        if (categoriesView == null || !i.Key.Contains('/'))
        {
            return;
        }
        
        if (!CategoriesTagsMap.ContainsKey(categoriesView.Id))
        {
            CategoriesTagsMap.Add(categoriesView.Id, new Dictionary<Guid, HashSet<string>>());
        }

        if (!CategoriesTagsMap[categoriesView.Id].ContainsKey(bucket.Id))
        {
            CategoriesTagsMap[categoriesView.Id].Add(bucket.Id, new HashSet<string>());
        }

        var s = new Stack<string>(i.Key.Split('/'));
        s.Pop();
        CategoriesTagsMap[categoriesView.Id][bucket.Id].AddRange(s);
    }

    private void OnCompleted()
    {
        var parameterDictList = new List<Dictionary<string, ParameterValueType>>();
        foreach (var (categoryId, tags) in CategoriesTagsMap)
        {
            var parameters = new Dictionary<string, ParameterValueType>
            {
                ["Id"] = new(categoryId),
                ["Name"] = new(""),
                ["Mimes"] = new(new List<string>()),
                ["Tags"] = new(tags)
            };
            parameterDictList!.Add(parameters);
        }

        var serializedDict = JsonConvert.SerializeObject(parameterDictList);
        _distributedCache.SetString("update.categories.parameters",
            serializedDict
        );
        
        Log.Logger.Information(
            "{Type}: Execute Succeeded, No Further Action Required",
            this.GetType().FullName
        );
    }

    private void OnError(Exception exception)
    {
        Log.Logger.Warning(
            "{Type}: Execute Failed With Following Message: {Message}",
            this.GetType().FullName,
            exception.Message
        );
    }
}