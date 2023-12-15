using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Pika.Domain.Storage.Callables;
using Pika.Domain.Storage.Callables.ValueTypes;
using PikaCore.Areas.Core.Models.File;
using PikaCore.Areas.Core.Queries;
using PikaCore.Infrastructure.Services;
using Serilog;

namespace PikaCore.Areas.Core.Callables;

public class RefreshCategoriesCallable : BaseJobCallable
{
    private readonly IDistributedCache _cache;
    private readonly IMediator _mediator;
    private readonly IMinioService _minioService;
    private readonly IMapper _mapper;

    public RefreshCategoriesCallable(IDistributedCache cache,
        IMediator mediator,
        IMinioService minioService,
        IMapper mapper) : base(cache)
    {
        this._cache = cache;
        this._mediator = mediator;
        this._minioService = minioService;
        this._mapper = mapper;
    }

    public override async Task Execute(Dictionary<string, ParameterValueType>? parameterValueTypes)
    {
        var buckets = await _mediator.Send(new GetAllBucketsQuery());
        var categories = (await _mediator.Send(new GetAllCategoriesQuery())).ToList();
        var bucketsToCategories = new Dictionary<string, List<string>>();
        foreach (var bucketsView in buckets)
        {
            var items = _minioService
                .ListObjects(bucketsView.Name, true);
            var categoryMap = 
                categories.ToDictionary(c => 
                    c.Id.ToString(), c => 
                    Enumerable.Empty<ObjectInfo>()
                    );
            items.Subscribe(
                item =>
                {
                    var mime = MimeTypes.GetMimeType(item.Key);
                    foreach (var category in categories)
                    {
                        var mimes = category.Mimes;
                        if (string.IsNullOrEmpty(mime) || !mimes.Contains(mime)) continue;
                        categoryMap[category.Id.ToString()] = 
                            categoryMap[category.Id.ToString()].Append(_mapper.Map<ObjectInfo>(item));
                    }
                },
                ex =>
                {
                    Log.Logger.Error(
                        "ListObjects: {Type} occured with following message: {Message}",
                        ex.GetType().FullName,
                        ex.Message
                    );
                }, 
                () =>
                {
                    Log.Logger.Information(
                        "{Type}: ListObjects: {Message}",
                        this.GetType().FullName,
                        "Operation Succeeded"
                    );
                    foreach (var (categoryId,categoryContents) in categoryMap)
                    {
                        _cache.SetAsync($"{bucketsView.Id}.category.contents.{categoryId}",
                            JsonSerializer.SerializeToUtf8Bytes(categoryContents));
                    } 
                    bucketsToCategories.Add(bucketsView.Id.ToString(), 
                        categoryMap.Keys
                        .Where(c => categoryMap[c].Any())
                        .ToList());
                    _cache.SetAsync($"buckets.categories.map", JsonSerializer.SerializeToUtf8Bytes(bucketsToCategories));
                });
        }
    }
}