using System.Collections.Generic;
using MediatR;
using Pika.Domain.Storage.Entity.View;

namespace PikaCore.Areas.Core.Queries;

public class GetBucketsForRoleQuery : IRequest<IEnumerable<BucketsView>>
{
    public string RoleString { get; set; }
}