using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Pika.Domain.Storage.Callables;
using Pika.Domain.Storage.Callables.ValueTypes;
using Pika.Domain.Storage.Entity;
using Pika.Domain.Storage.Repository;
using PikaCore.Areas.Core.Models.File;
using PikaCore.Infrastructure.Adapters.Minio;

namespace PikaCore.Areas.Core.Callables;

public class RefreshCategoriesCallable : BaseJobCallable
{
    private readonly IDistributedCache _cache;
    private readonly IMediator _mediator;
    private readonly AggregateRepository _aggregateRepository;
    private readonly IClientService _clientService;
    private readonly IConfiguration _configuration;
    private readonly IMapper _mapper;
    
    public RefreshCategoriesCallable(IDistributedCache cache,
        IMediator mediator,
        AggregateRepository aggregateRepository,
        IClientService clientService,
        IConfiguration configuration,
        IMapper mapper)
    {
        this._cache = cache;
        this._mediator = mediator;
        this._aggregateRepository = aggregateRepository;
        this._clientService = clientService;
        this._configuration = configuration;
        this._mapper = mapper;
    }
    
    public override async Task Execute(Dictionary<string, ParameterValueType>? parameterValueTypes)
    {
        var guid = await _cache.GetStringAsync("category.streamids");
        var gsList = JsonSerializer.Deserialize<List<Guid>>(guid);
        foreach (var gid in gsList)
        {
            try
            {
                var category = await _aggregateRepository.LoadAsync<Category>(gid);
                var items = await _clientService
                    .ListObjects(_configuration.GetSection("Minio")["Bucket"], true);
                var objectInfos = new List<ObjectInfo>();
                var mimes = category.Mimes;
                items.ToList().ForEach(i =>
                {
                    var mimeTypes = Winista.Mime.MimeTypes.Get(i.Key);
                    var mime = mimeTypes.GetMimeType(i.Key); 
                    if (mime is not null && mimes.Contains(mime.Name))
                    {
                        objectInfos.Add(_mapper.Map<ObjectInfo>(i));
                    }
                }); 
                await _cache.SetAsync($"category.contents.{gid}", JsonSerializer.SerializeToUtf8Bytes(objectInfos));
            }
            catch
            {
            }
        }
    }
}