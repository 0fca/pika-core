using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using PikaCore.Areas.Core.Models.DTO;
using PikaCore.Areas.Core.Models.File;

namespace PikaCore.Areas.Core.Queries;

public class FindAllObjectsByNameQueryHandler : IRequestHandler<FindAllObjectsByNameQuery, List<ObjectInfoDTO>>
{
    private readonly IDistributedCache _distributedCache;
    private readonly IMapper _mapper;
    
    public FindAllObjectsByNameQueryHandler(IDistributedCache distributedCache, IMapper mapper)
    {
        this._distributedCache = distributedCache;
        this._mapper = mapper;
    }
    
    public async Task<List<ObjectInfoDTO>> Handle(FindAllObjectsByNameQuery request, CancellationToken cancellationToken)
    {
        var serializedObjectInfos = await _distributedCache.GetStringAsync(
            $"{request.BucketId}.category.contents.{request.CategoryId}",
            cancellationToken);
        if (string.IsNullOrEmpty(serializedObjectInfos))
        {
            return new List<ObjectInfoDTO>{new()}; //TODO: Just for now, need to handle it properly
        }
        var objectInfos = JsonSerializer
            .Deserialize<IEnumerable<ObjectInfo>>(json: serializedObjectInfos)!
            .Where(info => info.Name.Contains(request.Name));
        return objectInfos!.ToList().ConvertAll(o => _mapper.Map<ObjectInfoDTO>(o));
    }
}