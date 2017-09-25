using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Linq;
using Realms;
using Toggl.Multivac;
using Toggl.Multivac.Models;
using Toggl.PrimeRadiant.Exceptions;

namespace Toggl.PrimeRadiant.Realm
{
    internal sealed class Repository<TModel> : BaseStorage<TModel>, IRepository<TModel>
    {
        public Repository(IRealmAdapter<TModel> adapter)
            : base(adapter) { }

        public IObservable<TModel> Create(TModel entity)
        {
            Ensure.Argument.IsNotNull(entity, nameof(entity));

            return Observable
                .Start(() => Adapter.Create(entity))
                .Catch<TModel, Exception>(ex => Observable.Throw<TModel>(new DatabaseException(ex)));
        }
        
        public IObservable<IEnumerable<(ConflictResolutionMode ResolutionMode, TModel Entity)>> BatchUpdate(
            IEnumerable<(long Id, TModel Entity)> entities,
            Func<TModel, TModel, ConflictResolutionMode> conflictResolution,
            IRivalsResolver<TModel> rivalsResolver = null)
        {
            Ensure.Argument.IsNotNull(entities, nameof(entities));
            Ensure.Argument.IsNotNull(conflictResolution, nameof(conflictResolution));

            return CreateObservable(() => Adapter.BatchUpdate(entities, conflictResolution, rivalsResolver));
        }

        public IObservable<TModel> GetById(long id)
            => CreateObservable(() => Adapter.Get(id));

        public static Repository<TModel> For<TRealmEntity>(
            Func<TModel, Realms.Realm, TRealmEntity> convertToRealm)
            where TRealmEntity : RealmObject, IBaseModel, TModel, IUpdatesFrom<TModel>
            => For(convertToRealm, matchById<TRealmEntity>);

        public static Repository<TModel> For<TRealmEntity>(
            Func<TModel, Realms.Realm, TRealmEntity> convertToRealm,
            Func<long, Expression<Func<TRealmEntity, bool>>> matchById)
            where TRealmEntity : RealmObject, TModel, IUpdatesFrom<TModel>
            => new Repository<TModel>(new RealmAdapter<TRealmEntity, TModel>(convertToRealm, matchById));

        private static Expression<Func<TRealmEntity, bool>> matchById<TRealmEntity>(long id)
            where TRealmEntity : RealmObject, IBaseModel, TModel, IUpdatesFrom<TModel>
            => x => x.Id == id;
    }
}
