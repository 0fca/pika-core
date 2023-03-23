using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Pika.Domain.Storage.Entity;
using Pika.Domain.Storage.Repository;

namespace PikaCore.Areas.Core.Commands;

public class UpdateCategoryHandler : IRequestHandler<UpdateCategoryCommand, Unit>
{
    private readonly AggregateRepository _aggregateRepository;
    public UpdateCategoryHandler(AggregateRepository aggregateRepository)
    {
        this._aggregateRepository = aggregateRepository;
    }
    
    public async Task<Unit> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        var c = await _aggregateRepository.LoadAsync<Category>(request.Guid, ct: cancellationToken);
        c.SetName(request.Name);
        c.SetDescription(request.Description);
        c.AddMimes(request.Mimes);
        c.AddTags(request.Tags);
        c.Update();
        await _aggregateRepository.StoreAsync(c, cancellationToken);
        return Unit.Value;
    }
}