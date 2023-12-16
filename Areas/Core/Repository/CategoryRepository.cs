using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Marten;
using Marten.Linq.SoftDeletes;
using Pika.Domain.Storage.Entity.View;
using Pika.Domain.Storage.Repository;

namespace PikaCore.Areas.Core.Repository;

public class CategoryRepository : AggregateRepository
{
    private readonly IDocumentStore _store;
    public CategoryRepository(IDocumentStore store) : base(store)
    {
        this._store = store;
    }

    public async Task<IEnumerable<CategoriesView>> GetAll()
    {
        await using var session = _store.LightweightSession();
        var e = session.Query<CategoriesView>();
        
        return e;
    } 
}