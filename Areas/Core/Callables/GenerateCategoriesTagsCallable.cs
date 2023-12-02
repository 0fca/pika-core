using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Minio.DataModel;
using Newtonsoft.Json;
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
        if (!await this.IsJobRunningOnMaster())
        {
            return;
        }

        var buckets = await _mediator.Send(new GetAllBucketsQuery());

        var categories = await _mediator.Send(new GetAllCategoriesQuery());
        var parameterDictList = new List<Dictionary<string, ParameterValueType>>();
        var tagsCategoriesMap = new Dictionary<Guid, Dictionary<Guid, HashSet<string>>>();
        await _distributedCache.SetStringAsync("update.categories.tags.map",
            JsonConvert.SerializeObject(tagsCategoriesMap));
        await _distributedCache.SetStringAsync("update.categories.parameters",
            JsonConvert.SerializeObject(parameterDictList));
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
    }

    private void OnNext(Item i, BucketsView bucket, IEnumerable<CategoriesView> categories)
    {
        var mime = MimeTypes.GetMimeType(i.Key);
        var tagsCategoriesMap = JsonConvert.DeserializeObject<Dictionary<Guid, Dictionary<Guid, HashSet<string>>>>(
            _distributedCache.GetString("update.categories.tags.map")!
        );
        var categoriesView = categories
            .FirstOrDefault(c => !string.IsNullOrEmpty(mime) || c.Mimes.Contains(mime));

        if (categoriesView == null)
        {
            return;
        }

        var tag = new List<string>();
        if (i.Key.Contains('/'))
        {
            var s = new Stack<string>(i.Key.Split('/'));
            s.Pop();
            tag.AddRange(s);
        }

        if (!tagsCategoriesMap.ContainsKey(categoriesView.Id))
        {
            tagsCategoriesMap.Add(categoriesView.Id, new Dictionary<Guid, HashSet<string>>());
        }

        if (!tagsCategoriesMap[categoriesView.Id].ContainsKey(bucket.Id))
        {
            tagsCategoriesMap[categoriesView.Id].Add(bucket.Id, new HashSet<string>());
        }

        tag.ForEach(t =>
        {
            if (!string.IsNullOrEmpty(t))
            {
                tagsCategoriesMap[categoriesView.Id][bucket.Id].Add(t);
            }
        });
        _distributedCache.SetString("update.categories.tags.map", JsonConvert.SerializeObject(tagsCategoriesMap));
    }

    private void OnCompleted()
    {
        var parameterDictList = new List<Dictionary<string, ParameterValueType>>();
        var tagsCategoriesMap = JsonConvert.DeserializeObject<Dictionary<Guid, Dictionary<Guid, HashSet<string>>>>(
            _distributedCache.GetString("update.categories.tags.map")!
        );
        foreach (var (categoryId, tags) in tagsCategoriesMap)
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
        var updateCallable = new UpdateCategoryCallable(_mediator, _distributedCache);
        var jobId = BackgroundJob.Schedule("default",
            () => updateCallable.Execute(null),
            TimeSpan.FromMilliseconds(500));
        _distributedCache.SetString("update.job.identifier", jobId,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(300)
            });
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