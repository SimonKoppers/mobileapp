using System;
using System.Linq;
using System.Linq.Expressions;

namespace Toggl.Foundation.Sync.ConflictResolution
{
    public interface IRivalsResolver<TModel>
    {
        bool CanHaveRival(TModel entity);
        Expression<Func<TModel, bool>> AreRivals(TModel entity);
        (TModel FixedEntity, TModel FixedRival) FixRivals(TModel entity, TModel rival, IQueryable<TModel> allEntities);
    }
}
