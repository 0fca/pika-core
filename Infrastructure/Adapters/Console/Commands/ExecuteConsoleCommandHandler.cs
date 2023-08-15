using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace PikaCore.Infrastructure.Adapters.Console.Commands;

public class ExecuteConsoleCommandHandler : IRequestHandler<ExecuteConsoleCommand, Guid>
{
    public Task<Guid> Handle(ExecuteConsoleCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}