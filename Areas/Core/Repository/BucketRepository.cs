using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Marten;
using Pika.Domain.Storage.Entity;
using Pika.Domain.Storage.Entity.Queries;
using Pika.Domain.Storage.Entity.View;
using Pika.Domain.Storage.Repository;

namespace PikaCore.Areas.Core.Repository;

public class BucketRepository : AggregateRepository
{
    private readonly IDocumentStore _store;
    public BucketRepository(IDocumentStore store) : base(store)
    {
        this._store = store;
    }

    public async Task<Bucket> FindByName(string name)
    {
        await using var session = _store.LightweightSession();
        var evts = await session.QueryAsync(new FindBucketByName()
        {
            BucketName = name
        });
        return evts;
    }

    public async Task<IEnumerable<BucketsView>> GetAllByRole(string roleString)
    {
        await using var session = _store.LightweightSession();
        var bucketsQuery = session.Query<BucketsView>()
            .Where(b => b.RoleClaims.Contains(roleString));
        return bucketsQuery.ToList();
    }
    
    public async Task<IEnumerable<BucketsView>> GetAll()
    {
        await using var session = _store.LightweightSession();
        var e = session.Query<BucketsView>().ToList();
        return e;
    } 
}