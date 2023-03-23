using System;
using System.Collections.Generic;
using MediatR;
using Pika.Domain.Storage.Entity;

namespace PikaCore.Areas.Core.Commands;

public class CreateBucketCommand : IRequest<Guid>
{
   public string Name { get; set; }
   public List<string> RoleClaims { get; set; }
}