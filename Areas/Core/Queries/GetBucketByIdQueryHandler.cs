using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Pika.Domain.Storage.Entity;
using Pika.Domain.Storage.Repository;

namespace PikaCore.Areas.Core.Queries;

public class GetBucketByIdQueryHandler : IRequestHandler<GetBucketByIdQuery, Bucket>
{
    private readonly AggregateRepository _aggregateRepository;
    public GetBucketByIdQueryHandler(
        AggregateRepository aggregateRepository) 
    {
        this._aggregateRepository = aggregateRepository;
    }
    
    public async Task<Bucket> Handle(GetBucketByIdQuery request, CancellationToken cancellationToken)
    {
        var c = await _aggregateRepository.LoadAsync<Bucket>(request.CategoryId(), ct: cancellationToken);
        return c;
    }
}