using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Pika.Domain.Storage.Entity.View;
using PikaCore.Areas.Core.Repository;
using PikaCore.Infrastructure.Adapters.Minio;
using PikaCore.Infrastructure.Services;

namespace PikaCore.Areas.Core.Queries;

public class GetAllCategoriesQueryHandler : IRequestHandler<GetAllCategoriesQuery, IEnumerable<CategoriesView>>
{
    private readonly IMinioService _minioService;
    private readonly IConfiguration _configuration;
    private readonly IDistributedCache _distributedCache;
    private readonly IMapper _mapper;
    private readonly CategoryRepository _aggregateRepository;
    public GetAllCategoriesQueryHandler(IMinioService service, 
        IConfiguration configuration, 
        IDistributedCache distributedCache,
        IMapper mapper,
        CategoryRepository aggregateRepository) 
    {
        this._minioService = service;
        this._configuration = configuration;
        this._distributedCache = distributedCache;
        this._mapper = mapper;
        this._aggregateRepository = aggregateRepository;
    }
    public async Task<IEnumerable<CategoriesView>> Handle(GetAllCategoriesQuery request, CancellationToken cancellationToken)
    {
        var categories = await _aggregateRepository.GetAll();
        return new List<CategoriesView>(categories);
    }
}