using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Pika.Domain.Storage.Entity.View;
using PikaCore.Areas.Core.Repository;

namespace PikaCore.Areas.Core.Queries;

public class GetAllBucketsQueryHandler : IRequestHandler<GetAllBucketsQuery, IEnumerable<BucketsView>>
{
    private readonly BucketRepository _aggregateRepository;
    public GetAllBucketsQueryHandler(BucketRepository aggregateRepository) 
    {
        this._aggregateRepository = aggregateRepository;
    }
    public async Task<IEnumerable<BucketsView>> Handle(GetAllBucketsQuery request, CancellationToken cancellationToken)
    {
        return await _aggregateRepository.GetAll();
    }
}