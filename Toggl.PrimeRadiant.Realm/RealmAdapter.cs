using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Realms;
using Toggl.Multivac;

namespace Toggl.PrimeRadiant.Realm
{
    internal interface IRealmAdapter<TModel>
    {
        IQueryable<TModel> GetAll();

        TModel Get(long id);

        void Delete(long id);

        TModel Create(TModel entity);

        TModel Update(long id, TModel entity);

        IEnumerable<(ConflictResolutionMode ResolutionMode, TModel Entity)> BatchUpdate(
            IEnumerable<(long Id, TModel Entity)> batch,
            Func<TModel, TModel, ConflictResolutionMode> conflictResolution,
            IRivalsResolver<TModel> rivalsResolver);
    }

    internal sealed class RealmAdapter<TRealmEntity, TModel> : IRealmAdapter<TModel>
        where TRealmEntity : RealmObject, TModel, IUpdatesFrom<TModel>
    {
        private readonly Func<TModel, Realms.Realm, TRealmEntity> clone;

        private readonly Func<long, Expression<Func<TRealmEntity, bool>>> matchEntity;

        public RealmAdapter(Func<TModel, Realms.Realm, TRealmEntity> clone, Func<long, Expression<Func<TRealmEntity, bool>>> matchEntity)
        {
            Ensure.Argument.IsNotNull(clone, nameof(clone));
            Ensure.Argument.IsNotNull(matchEntity, nameof(matchEntity));

            this.clone = clone;
            this.matchEntity = matchEntity;
        }

        public IQueryable<TModel> GetAll()
            => Realms.Realm.GetInstance().All<TRealmEntity>();

        public TModel Get(long id)
            => Realms.Realm.GetInstance().All<TRealmEntity>().Single(matchEntity(id));

        public TModel Create(TModel entity)
        {
            Ensure.Argument.IsNotNull(entity, nameof(entity));

            return doTransaction(realm => realm.Add(convertToRealm(entity, realm)));
        }
        
        public TModel Update(long id, TModel entity)
        {
            Ensure.Argument.IsNotNull(entity, nameof(entity));

            return doModyfingTransaction(id, (realm, realmEntity) => realmEntity.SetPropertiesFrom(entity, realm));
        }

        public IEnumerable<(ConflictResolutionMode ResolutionMode, TModel Entity)> BatchUpdate(
            IEnumerable<(long Id, TModel Entity)> batch,
            Func<TModel, TModel, ConflictResolutionMode> conflictResolution,
            IRivalsResolver<TModel> rivalsResolver)
        {
            Ensure.Argument.IsNotNull(batch, nameof(batch));
            Ensure.Argument.IsNotNull(matchEntity, nameof(matchEntity));
            Ensure.Argument.IsNotNull(conflictResolution, nameof(conflictResolution));

            var realm = Realms.Realm.GetInstance();
            using (var transaction = realm.BeginWrite())
            {
                var realmEntities = realm.All<TRealmEntity>();
                var entitiesWithPotentialRivals = new List<TRealmEntity>();
                var resolvedEntities = batch.Select(updated =>
                {
                    var oldEntity = realmEntities.SingleOrDefault(matchEntity(updated.Id));
                    var resolveMode = conflictResolution(oldEntity, updated.Entity);
                    var resolvedEntity = resolveEntity(realm, oldEntity, updated.Entity, resolveMode);

                    if (rivalsResolver != null &&
                        (resolveMode == ConflictResolutionMode.Create || resolveMode == ConflictResolutionMode.Update) &&
                        rivalsResolver.CanHaveRival(resolvedEntity))
                    {
                        entitiesWithPotentialRivals.Add(resolvedEntity);
                    }

                    return (resolveMode, (TModel)resolvedEntity);
                }).ToList();

                foreach (var maybeHasRival in entitiesWithPotentialRivals)
                {
                    resolvePotentialRivals(realm, maybeHasRival, rivalsResolver, resolvedEntities);
                }

                transaction.Commit();
                return resolvedEntities;
            }
        }

        public void Delete(long id)
        {
            doModyfingTransaction(id, (realm, realmEntity) => realm.Remove(realmEntity));
        }

        private TRealmEntity convertToRealm(TModel entity, Realms.Realm realm)
            => entity as TRealmEntity ?? clone(entity, realm);

        private TModel doModyfingTransaction(long id, Action<Realms.Realm, TRealmEntity> transact)
            => doTransaction(realm =>
            {
                var realmEntity = realm.All<TRealmEntity>().Single(matchEntity(id));
                transact(realm, realmEntity);
                return realmEntity;
            });

        private TModel doTransaction(Func<Realms.Realm, TModel> transact)
        {
            var realm = Realms.Realm.GetInstance();
            using (var transaction = realm.BeginWrite())
            {
                var returnValue = transact(realm);
                transaction.Commit();
                return returnValue;
            }
        }

        private TRealmEntity resolveEntity(Realms.Realm realm, TRealmEntity old, TModel entity, ConflictResolutionMode resolveMode)
        {
            switch (resolveMode)
            {
                case ConflictResolutionMode.Create:
                    return realm.Add(convertToRealm(entity, realm));

                case ConflictResolutionMode.Delete:
                    var ghost = clone(old, realm);
                    realm.Remove(old);
                    return ghost;

                case ConflictResolutionMode.Update:
                    old.SetPropertiesFrom(entity, realm);
                    return old;

                case ConflictResolutionMode.Ignore:
                    return old;

                default:
                    throw new ArgumentException($"Unknown conflict resolution mode {resolveMode}");
            }
        }

        private void resolvePotentialRivals(
            Realms.Realm realm,
            TRealmEntity entity,
            IRivalsResolver<TModel> resolver,
            List<(ConflictResolutionMode, TModel)> resolvedEntities)
        {
            var rival = (TRealmEntity)realm.All<TRealmEntity>().SingleOrDefault(resolver.AreRivals(entity));
            if (rival != null)
            {
                (TModel fixedEntity, TModel fixedRival) = resolver.FixRivals(entity, rival);
                entity.SetPropertiesFrom(fixedEntity, realm);
                rival.SetPropertiesFrom(fixedRival, realm);
                resolvedEntities.Add((ConflictResolutionMode.Update, rival));
            }
        }
    }
}
