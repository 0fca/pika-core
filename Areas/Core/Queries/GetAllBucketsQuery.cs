using System.Collections.Generic;
using MediatR;
using Pika.Domain.Storage.Entity;
using Pika.Domain.Storage.Entity.View;
using PikaCore.Areas.Core.Queries.Enums;

namespace PikaCore.Areas.Core.Queries;

public class GetAllBucketsQuery : IRequest<IEnumerable<BucketsView>>
{
    public OrderByOptions OrderBy { get; set; }
}