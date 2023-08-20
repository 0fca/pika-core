using System;
using System.Collections.Generic;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace PikaCore.Infrastructure.Adapters.Filesystem.Commands;

public class SanitizeTemporaryFileCommand : IRequest<Guid>
{
    public SanitizeTemporaryFileCommand(IList<IFormFile> formFiles, string bucketId)
    {
        FormFiles = formFiles;
        BucketId = bucketId;
        this.Id = Guid.NewGuid();
    }
    
    public Guid Id { get; }
    
    public IList<IFormFile> FormFiles { get; set; }
    public string BucketId { get; set; }
}