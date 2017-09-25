using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive;

namespace Toggl.PrimeRadiant
{
    public interface IRepository<TModel>
    {
        IObservable<TModel> GetById(long id);
        IObservable<TModel> Create(TModel entity);
        IObservable<TModel> Update(long id, TModel entity);
        IObservable<IEnumerable<(ConflictResolutionMode ResolutionMode, TModel Entity)>> BatchUpdate(
            IEnumerable<(long Id, TModel Entity)> entities,
            Func<TModel, TModel, ConflictResolutionMode> conflictResolution,
            IRivalsResolver<TModel> rivalsResolver = null);
        IObservable<Unit> Delete(long id);
        IObservable<IEnumerable<TModel>> GetAll();
        IObservable<IEnumerable<TModel>> GetAll(Func<TModel, bool> predicate);
    }
}
