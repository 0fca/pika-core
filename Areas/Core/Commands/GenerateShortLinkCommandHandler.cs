using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Pika.Domain.Storage.Data;
using PikaCore.Areas.Core.Data;

namespace PikaCore.Areas.Core.Commands;

public class GenerateShortLinkCommandHandler : IRequestHandler<GenerateShortLinkCommand, Guid>
{
    private readonly StorageIndexContext _storageIndexContext;

    public GenerateShortLinkCommandHandler(StorageIndexContext storageIndexContext)
    {
        _storageIndexContext = storageIndexContext;
    }

    public async Task<Guid> Handle(GenerateShortLinkCommand request, CancellationToken cancellationToken)
    {
        var s = _storageIndexContext.IndexStorage.ToList()
            .Find(record => record.ObjectName.Equals(request.ObjectName) 
                            && record.BucketId.Equals(request.BucketId));

        if (s == null)
        {
            s = new StorageIndexRecord
            {
                ObjectName = request.ObjectName,
                BucketId = request.BucketId,
                Hash = GetHash($"{request.ObjectName}{request.BucketId}"), 
                Expires = true
            };
        }
        else if (s.ExpireDate.Date <= DateTime.Now.Date)
        {
            s.ExpireDate = StorageIndexRecord.ComputeDateTime();
        }

        _storageIndexContext.Update(s);
        await _storageIndexContext.SaveChangesAsync(cancellationToken);
        return request.Id;
    }

    private static string GetHash(string inputString)
    {
        return System.Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(inputString)));
    }
}