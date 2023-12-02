using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Pika.Domain.Storage.Data;
using PikaCore.Areas.Core.Data;
using PikaCore.Areas.Core.Services;

namespace PikaCore.Areas.Core.Commands;

public class GenerateShortLinkCommandHandler : IRequestHandler<GenerateShortLinkCommand, Guid>
{
    private readonly StorageIndexContext _storageIndexContext;
    private readonly IHashGenerator _hashGenerator;

    private readonly IDistributedCache _distributedCache;

    public GenerateShortLinkCommandHandler(StorageIndexContext storageIndexContext,
        IHashGenerator hashGenerator,
        IDistributedCache distributedCache)
    {
        _storageIndexContext = storageIndexContext;
        _hashGenerator = hashGenerator;
        this._distributedCache = distributedCache;
    }

    public async Task<Guid> Handle(GenerateShortLinkCommand request, CancellationToken cancellationToken)
    {
        var s = _storageIndexContext.IndexStorage.ToList()
                    .Find(record => record.ObjectName.Equals(request.ObjectName)
                                    && record.BucketId.Equals(request.BucketId))
                ?? new StorageIndexRecord
                {
                    ObjectName = request.ObjectName,
                    BucketId = request.BucketId,
                    Hash = _hashGenerator.GenerateId($"{request.ObjectName}{request.BucketId}"),
                    Expires = true,
                    ExpireDate = DateTime.UtcNow.AddDays(10).ToUniversalTime()
                };

        _storageIndexContext.Update(s);
        await _storageIndexContext.SaveChangesAsync(cancellationToken);
        await _distributedCache.SetAsync(
            request.Id.ToString(),
            JsonSerializer.SerializeToUtf8Bytes(s.Hash),
            token: cancellationToken
        );
        return request.Id;
    }
}