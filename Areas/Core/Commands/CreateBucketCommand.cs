using System;
using System.Collections.Generic;
using MediatR;

namespace PikaCore.Areas.Core.Commands;

public class CreateBucketCommand : IRequest<Guid>
{
   public string Name { get; set; }
   public List<string> RoleClaims { get; set; }
}