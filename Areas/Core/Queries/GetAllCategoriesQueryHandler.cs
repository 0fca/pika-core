using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Pika.Domain.Storage.Entity;
using Pika.Domain.Storage.Repository;
using PikaCore.Areas.Core.Models.File;
using PikaCore.Infrastructure.Adapters.Minio;

namespace PikaCore.Areas.Core.Queries;

public class GetAllCategoriesQueryHandler : IRequestHandler<GetAllCategoriesQuery, List<Category>>
{
    private readonly IClientService _clientService;
    private readonly IConfiguration _configuration;
    private readonly IDistributedCache _distributedCache;
    private readonly IMapper _mapper;
    private readonly AggregateRepository _aggregateRepository;
    public GetAllCategoriesQueryHandler(IClientService service, 
        IConfiguration configuration, 
        IDistributedCache distributedCache,
        IMapper mapper,
        AggregateRepository aggregateRepository) 
    {
        this._clientService = service;
        this._configuration = configuration;
        this._distributedCache = distributedCache;
        this._mapper = mapper;
        this._aggregateRepository = aggregateRepository;
    }
    public async Task<List<Category>> Handle(GetAllCategoriesQuery request, CancellationToken cancellationToken)
    {
        var guid = await _distributedCache.GetStringAsync("category.streamids", cancellationToken);
        var gsList = JsonSerializer.Deserialize<List<Guid>>(guid);
        var objectInfos = new List<Category>();
        foreach (var gid in gsList)
        {
            try
            {
                objectInfos.Add(await _aggregateRepository.LoadAsync<Category>(gid, ct: cancellationToken));
            }
            catch (InvalidOperationException e)
            {
            }
        }
        return objectInfos;
    }
}