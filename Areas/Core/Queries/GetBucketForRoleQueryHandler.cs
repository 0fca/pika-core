using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Pika.Domain.Storage.Entity.View;
using PikaCore.Areas.Core.Repository;

namespace PikaCore.Areas.Core.Queries;

public class GetBucketForRoleQueryHandler : IRequestHandler<GetBucketsForRoleQuery, IEnumerable<BucketsView>>
{
    private readonly BucketRepository _aggregateRepository;
    private readonly IMapper _mapper;

    public GetBucketForRoleQueryHandler(BucketRepository aggregateRepository,
        IMapper mapper)
    {
        this._aggregateRepository = aggregateRepository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<BucketsView>> Handle(GetBucketsForRoleQuery request,
        CancellationToken cancellationToken)
    {
        var buckets = await _aggregateRepository.GetAllByRole(request.RoleString);
        return buckets.ToList();
    }
}