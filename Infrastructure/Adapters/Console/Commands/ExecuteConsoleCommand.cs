using System;
using System.Collections.Generic;
using MediatR;

namespace PikaCore.Infrastructure.Adapters.Console.Commands;

public class ExecuteConsoleCommand : IRequest<Guid>
{
    public Guid Id { get; set; }
    public string CommandName { get; set; }
    public HashSet<string> Headers { get; set; }
    public string Body { get; set; }
}