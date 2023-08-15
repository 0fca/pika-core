using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Pika.Domain.Storage.Entity.View;
using PikaCore.Areas.Core.Repository;

namespace PikaCore.Infrastructure.Adapters.Console.Queries;

public class FindCommandByNameQueryHandler : IRequestHandler<FindCommandByNameQuery, CommandsView>
{
    private readonly CommandRepository _aggregateRepository;
    public FindCommandByNameQueryHandler(
        CommandRepository aggregateRepository) 
    {
        this._aggregateRepository = aggregateRepository;
    }
    
    public async Task<CommandsView> Handle(FindCommandByNameQuery request, CancellationToken cancellationToken)
    {
        var commands = await _aggregateRepository.FindSingle(request.Name());
        return commands;
    }
}