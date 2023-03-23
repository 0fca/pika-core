using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Pika.Domain.Storage.Entity;
using Pika.Domain.Storage.Repository;

namespace PikaCore.Areas.Core.Commands;

public class CreateCategoryHandler : IRequestHandler<CreateCategoryCommand, Guid>
{
    private readonly AggregateRepository _aggregateRepository;
    public CreateCategoryHandler(AggregateRepository aggregateRepository)
    {
        this._aggregateRepository = aggregateRepository;
    }
    
    public async Task<Guid> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        var c = new Category(request.Name, request.Description, request.Mimes);
        await _aggregateRepository.StoreAsync(c, cancellationToken);
        return c.Id;
    } 
}