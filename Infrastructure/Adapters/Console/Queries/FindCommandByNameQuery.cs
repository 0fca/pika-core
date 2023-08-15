using MediatR;
using Pika.Domain.Storage.Entity.View;

namespace PikaCore.Infrastructure.Adapters.Console.Queries;

public class FindCommandByNameQuery : IRequest<CommandsView>
{
   private readonly string _name;

   public FindCommandByNameQuery(string name)
   {
      this._name = name;
   }

   public string Name()
   {
      return this._name;
   }
}