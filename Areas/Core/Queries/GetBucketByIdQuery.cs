using System;
using MediatR;
using Pika.Domain.Storage.Entity;

namespace PikaCore.Areas.Core.Queries;

public class GetBucketByIdQuery : IRequest<Bucket>
{
    private readonly Guid guid;
    public GetBucketByIdQuery(Guid guid)
    {
        this.guid = guid;
    }

    public Guid CategoryId()
    {
        return guid;
    }
}