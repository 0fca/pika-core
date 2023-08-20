using System;
using MediatR;

namespace PikaCore.Areas.Core.Commands;

public class GenerateShortLinkCommand : IRequest<Guid>
{
    public GenerateShortLinkCommand()
    {
        this.Id = Guid.NewGuid();
    }
    
    public Guid Id { get; set; }
    public string ObjectName { get; set; }
    public string BucketId { get; set; }
}