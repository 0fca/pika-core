using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Pika.Domain.Storage.Data;
using PikaCore.Areas.Core.Data;

namespace PikaCore.Areas.Core.Queries;
public class FindShortLinkByHashQueryHandler : IRequestHandler<FindShortLinkByHashQuery, StorageIndexRecord>
{
    private readonly StorageIndexContext _storageIndexContext;

    public FindShortLinkByHashQueryHandler(
        StorageIndexContext storageIndexContext
    )
    {
        this._storageIndexContext = storageIndexContext;
    }

    public async Task<StorageIndexRecord> Handle(FindShortLinkByHashQuery request, CancellationToken cancellationToken)
    {
        var storageIndexRecord = await _storageIndexContext.IndexStorage
            .FirstOrDefaultAsync(s =>
                    s.Hash.Equals(request.Hash),
                cancellationToken: cancellationToken
            );
        if (!storageIndexRecord!.Expires)
        {
            return storageIndexRecord;
        }
        if (storageIndexRecord!.ExpireDate.Subtract(DateTime.UtcNow).Days > 0)
        {
            return storageIndexRecord;
        } 
        throw new ApplicationException("No link found!");
    }
}