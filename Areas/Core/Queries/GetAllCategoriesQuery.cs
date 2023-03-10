using System.Collections.Generic;
using MediatR;
using Pika.Domain.Storage.Entity;
using PikaCore.Areas.Core.Queries.Enums;

namespace PikaCore.Areas.Core.Queries;

public class GetAllCategoriesQuery : IRequest<List<Category>>
{ 
    public OrderByOptions OrderBy { get; set; }
}