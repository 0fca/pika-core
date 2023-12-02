using System;
using System.Collections.Generic;
using MediatR;
using PikaCore.Areas.Core.Models.DTO;
using PikaCore.Areas.Core.Models.File;

namespace PikaCore.Areas.Core.Queries;

public class FindAllObjectsByNameQuery : IRequest<List<ObjectInfoDTO>>
{
   public FindAllObjectsByNameQuery(string name, string categoryId, string bucketId)
   {
       this.Id = Guid.NewGuid();
       this.Name = name;
       this.CategoryId = categoryId;
       this.BucketId = bucketId;
   }
   public Guid Id { get;  }
   public string Name { get;  }
   public string CategoryId { get; set; }
   public string BucketId { get; set; }
}