using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Pika.Domain.Storage.Entity;
using Pika.Domain.Storage.Repository;

namespace PikaCore.Areas.Core.Commands;

public class CreateBucketHandler : IRequestHandler<CreateBucketCommand, Guid>
{
    private readonly AggregateRepository _aggregateRepository;
    public CreateBucketHandler(AggregateRepository aggregateRepository)
    {
        this._aggregateRepository = aggregateRepository;
    }
    
    public async Task<Guid> Handle(CreateBucketCommand request, CancellationToken cancellationToken)
    {
        var bucket = new Bucket(request.Name, request.RoleClaims); 
        await _aggregateRepository.StoreAsync(bucket, cancellationToken);
        return bucket.Id;
    }
}