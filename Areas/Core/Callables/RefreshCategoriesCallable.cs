using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Pika.Domain.Storage.Callables;
using Pika.Domain.Storage.Callables.ValueTypes;
using PikaCore.Areas.Core.Models.File;
using PikaCore.Areas.Core.Queries;
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
        foreach (var bucketsView in buckets)
        {
            var bucketsCategories = new List<string>();
            var items = await _minioService
                .ListObjects(bucketsView.Name, true);
            foreach (var category in categories)
            {
                var mimes = category.Mimes;
                var objectInfos = new List<ObjectInfo>();
                var itemsChecked = new List<int>();
                items.ToList().ForEach(i =>
                {
                    var mime = MimeTypes.GetMimeType(i.Key);
                    if (string.IsNullOrEmpty(mime) || !mimes.Contains(mime)) return;
                    var oi = _mapper.Map<ObjectInfo>(i);
                    oi.MimeType = mime;
                    objectInfos.Add(oi);
                    itemsChecked.Add(items.IndexOf(i));
                });
                itemsChecked.Reverse();
                itemsChecked.ForEach(ic =>
                {
                    items.RemoveAt(ic);
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