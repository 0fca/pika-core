using System;
using MediatR;

namespace PikaCore.Areas.Core.Commands;

public class GenerateShortLinkCommand : IRequest<Guid>
{
    public GenerateShortLinkCommand(string objectName, string bucketId)
    {
        this.Id = Guid.NewGuid();
        this.ObjectName = objectName;
        this.BucketId = bucketId;
    }
    
    public Guid Id { get; set; }
    public string ObjectName { get; set; }
    public string BucketId { get; set; }
}