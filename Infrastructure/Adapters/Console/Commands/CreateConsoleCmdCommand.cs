using System;
using System.Collections.Generic;
using MediatR;

namespace PikaCore.Infrastructure.Adapters.Console.Commands;

public class CreateConsoleCmdCommand : IRequest<Guid>
{
   public CreateConsoleCmdCommand(string name, HashSet<string> headers, string body)
   {
      this.Id = Guid.NewGuid();
      this.Name = name;
      this.Body = body;
      this.Headers = headers;
   } 
   
   public Guid Id { get; set; }
   public string Name { get; set; }
   public HashSet<string> Headers { get; set; }
   public string Body { get; set; }
}