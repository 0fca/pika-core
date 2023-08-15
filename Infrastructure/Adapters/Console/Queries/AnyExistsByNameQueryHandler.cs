using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PikaCore.Areas.Core.Repository;

namespace PikaCore.Infrastructure.Adapters.Console.Queries;

public class AnyExistsByNameQueryHandler : IRequestHandler<AnyExistsByNameQuery, bool>
{
    private readonly CommandRepository _aggregateRepository;
    public AnyExistsByNameQueryHandler(
        CommandRepository aggregateRepository) 
    {
        this._aggregateRepository = aggregateRepository;
    }
    
    public async Task<bool> Handle(AnyExistsByNameQuery request, CancellationToken cancellationToken)
    {
        var commands = await _aggregateRepository.AnyByName(request.Name());
        return commands;
    }
}