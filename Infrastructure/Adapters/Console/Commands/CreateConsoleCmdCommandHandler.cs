using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Pika.Domain.Storage.Entity;
using Pika.Domain.Storage.Repository;

namespace PikaCore.Infrastructure.Adapters.Console.Commands;

public class CreateConsoleCmdCommandHandler : IRequestHandler<CreateConsoleCmdCommand, Guid>
{
    private readonly AggregateRepository _aggregateRepository;

    public CreateConsoleCmdCommandHandler(AggregateRepository aggregateRepository)
    {
        this._aggregateRepository = aggregateRepository;
    }
    
    public async Task<Guid> Handle(CreateConsoleCmdCommand request, CancellationToken cancellationToken)
    {
        var command = new Command(request.Name, request.Headers, request.Body);
        await _aggregateRepository.StoreAsync(command, cancellationToken);
        return command.Id;
    }
}