using System;
using System.Collections.Generic;
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
            Func<TModel, TModel, ConflictResolutionMode> conflictResolution);
        IObservable<IEnumerable<(ConflictResolutionMode ResolutionMode, TModel Entity)>> BatchUpdate(
            IEnumerable<(long Id, TModel Entity)> batch,
            Func<TModel, TModel, ConflictResolutionMode> conflictResolution,
            Func<TModel, bool> canHaveRival,
            Func<TModel, Expression<Func<TModel, bool>>> areRivals,
            Func<TModel, TModel, (TModel FixedEntity, TModel FixedRival)> fixRivals);
        IObservable<Unit> Delete(long id);
        IObservable<IEnumerable<TModel>> GetAll();
        IObservable<IEnumerable<TModel>> GetAll(Func<TModel, bool> predicate);
    }
}
