using System;
using MediatR;
using Pika.Domain.Storage.Entity;

namespace PikaCore.Areas.Core.Queries;

public class GetCategoryByIdQuery : IRequest<Category>
{
    private readonly Guid guid;
    public GetCategoryByIdQuery(Guid guid)
    {
        this.guid = guid;
    }

    public Guid CategoryId()
    {
        return guid;
    }
}