using System;
using System.Collections.Generic;
using MediatR;
using PikaCore.Areas.Core.Models.File;
using PikaCore.Areas.Core.Queries.Enums;

namespace PikaCore.Areas.Core.Queries;

public class GetAllObjectsByCategoryQuery : IRequest<List<ObjectInfo>>
{
    private readonly Guid guid;
    public GetAllObjectsByCategoryQuery(Guid guid)
    {
        this.guid = guid;
    }
    public OrderByOptions OrderBy { get; set; }

    public Guid CategoryId()
    {
        return guid;
    }
}