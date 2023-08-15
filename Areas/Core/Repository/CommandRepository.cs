using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Marten;
using Pika.Domain.Storage.Entity.View;
using Pika.Domain.Storage.Repository;

namespace PikaCore.Areas.Core.Repository;

public class CommandRepository : AggregateRepository
{
    private readonly IDocumentStore _store;
    public CommandRepository(IDocumentStore store) : base(store)
    {
        this._store = store;
    }

    public async Task<CommandsView?> FindSingle(string name)
    {
        await using var session = _store.LightweightSession();
        var commandsQuery = session.Query<CommandsView>()
            .Where(b => b.Name.Equals(name));
        return commandsQuery.FirstOrDefault();
    }

    public async Task<bool> AnyByName(string name)
    {
        await using var session = _store.LightweightSession();
        var commandsQuery = session.Query<CommandsView>()
            .Any(b => b.Name.Equals(name));
        return commandsQuery; 
    }
}