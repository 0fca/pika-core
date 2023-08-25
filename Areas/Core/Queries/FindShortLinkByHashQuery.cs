using System;
using MediatR;
using Pika.Domain.Storage.Data;

namespace PikaCore.Areas.Core.Queries;

public class FindShortLinkByHashQuery : IRequest<StorageIndexRecord>
{
   public Guid Id { get; set; }

   public string Hash { get; set; }
   
   public FindShortLinkByHashQuery(string hash)
   {
      this.Id = Guid.NewGuid();
      this.Hash = hash;
   }
}