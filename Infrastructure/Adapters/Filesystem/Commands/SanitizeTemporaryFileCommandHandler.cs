using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Configuration;
using PikaCore.Areas.Core.Queries;
using PikaCore.Infrastructure.Services;

namespace PikaCore.Infrastructure.Adapters.Filesystem.Commands;

public class SanitizeTemporaryFileCommandHandler : IRequestHandler<SanitizeTemporaryFileCommand, Guid>
{
    private readonly IConfiguration _configuration;
    private readonly IMinioService _minioService;
    private readonly IMediator _mediator;

    public SanitizeTemporaryFileCommandHandler(
        IConfiguration configuration, 
        IMinioService minioService, 
        IMediator mediator)
    {
        this._configuration = configuration;
        _minioService = minioService;
        _mediator = mediator;
    }

    public async Task<Guid> Handle(SanitizeTemporaryFileCommand request, CancellationToken cancellationToken)
    {
        var paths = request.FormFiles;
        var bucket = await _mediator.Send(new GetBucketByIdQuery(Guid.Parse(request.BucketId)), cancellationToken);
        paths.ToList().ForEach(ff =>
        {
            var permittedMimes = 
                _configuration.GetSection("Storage:PermittedMimes").Get<List<string>>() 
                ?? new List<string>();
            var permittedExtensions = 
                _configuration.GetSection("Storage:PermittedExtensions").Get<List<string>>() 
                ?? new List<string>();
            FileSecurityHelper.ProcessTemporaryStoredFile(
                Path.GetFileName(ff.FileName),
                ff.OpenReadStream(),
                permittedExtensions,
                permittedMimes
            );
            _minioService.PutObject(ff.FileName, ff.OpenReadStream(), bucket.Name);
        });
        return request.Id;
    }
}