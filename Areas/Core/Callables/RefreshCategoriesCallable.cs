using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Pika.Domain.Storage.Callables;
using Pika.Domain.Storage.Callables.ValueTypes;
using PikaCore.Areas.Core.Models.File;
using PikaCore.Areas.Core.Queries;
using PikaCore.Infrastructure.Adapters.Minio;
using PikaCore.Infrastructure.Services;

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
        IMapper mapper)
    {
        this._cache = cache;
        this._mediator = mediator;
        this._minioService = minioService;
        this._mapper = mapper;
    }

    public override async Task Execute(Dictionary<string, ParameterValueType>? parameterValueTypes)
    {
        var buckets = await _mediator.Send(new GetAllBucketsQuery());
        var categories = await _mediator.Send(new GetAllCategoriesQuery());
        var bucketsToCategories = new Dictionary<string, List<string>>();
        var mimeTypes = new Winista.Mime.MimeTypes();
        foreach (var bucketsView in buckets)
        {
            var bucketsCategories = new List<string>();
            var items = await _minioService
                .ListObjects(bucketsView.Name, true);
            foreach (var category in categories)
            {
                var mimes = category.Mimes;
                var objectInfos = new List<ObjectInfo>();
                items.ToList().ForEach(i =>
                {
                    var mime = mimeTypes.GetMimeType(i.Key);
                    if (mime is not null && mimes.Contains(mime.Name))
                    {
                        objectInfos.Add(_mapper.Map<ObjectInfo>(i));
                    }
                });
                bucketsCategories.Add(category.Id.ToString());
                await _cache.SetAsync($"{bucketsView.Id}.category.contents.{category.Id}",
                    JsonSerializer.SerializeToUtf8Bytes(objectInfos));
            }

            bucketsToCategories.Add(bucketsView.Id.ToString(), bucketsCategories);
        }

        await _cache.SetAsync($"buckets.categories.map", JsonSerializer.SerializeToUtf8Bytes(bucketsToCategories));
    }
}