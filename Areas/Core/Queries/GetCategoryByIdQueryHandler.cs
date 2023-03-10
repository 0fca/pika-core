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

public class GetCategoryByIdQueryHandler : IRequestHandler<GetCategoryByIdQuery, Category>
{
    private readonly AggregateRepository _aggregateRepository;
    public GetCategoryByIdQueryHandler(
        AggregateRepository aggregateRepository) 
    {
        this._aggregateRepository = aggregateRepository;
    }
    
    public async Task<Category> Handle(GetCategoryByIdQuery request, CancellationToken cancellationToken)
    {
        var c = await _aggregateRepository.LoadAsync<Category>(request.CategoryId(), ct: cancellationToken);
        return c;
    }
}