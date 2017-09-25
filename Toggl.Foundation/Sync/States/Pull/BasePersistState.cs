using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Toggl.Foundation.Sync.ConflictResolution;
using Toggl.Multivac.Models;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Models;

namespace Toggl.Foundation.Sync.States
{
    internal abstract class BasePersistState<TInterface, TDatabaseInterface>
        where TDatabaseInterface : TInterface
    {
        private readonly IRepository<TDatabaseInterface> repository;

        private readonly ISinceParameterRepository sinceParameterRepository;

        private readonly IConflictResolver<TDatabaseInterface> conflictResolver;

        private readonly IRivalsResolver<TDatabaseInterface> rivalsResolver;

        public StateResult<FetchObservables> FinishedPersisting { get; } = new StateResult<FetchObservables>();

        protected BasePersistState(
            IRepository<TDatabaseInterface> repository,
            ISinceParameterRepository sinceParameterRepository,
            IConflictResolver<TDatabaseInterface> conflictResolver,
            IRivalsResolver<TDatabaseInterface> rivalsResolver = null)
        {
            this.repository = repository;
            this.sinceParameterRepository = sinceParameterRepository;
            this.conflictResolver = conflictResolver;
            this.rivalsResolver = rivalsResolver;
        }

        public IObservable<ITransition> Start(FetchObservables fetch)
        {
            var since = fetch.SinceParameters;
            return FetchObservable(fetch)
                .SingleAsync()
                .Select(entities => entities ?? new List<TInterface>())
                .Select(entities => entities.Select(ConvertToDatabaseEntity).ToList())
                .SelectMany(databaseEntities =>
                    repository.BatchUpdate(databaseEntities.Select(entity => (GetId(entity), entity)), conflictResolver.Resolve, rivalsResolver)
                        .Select(results => results.Select(result => result.Item2))
                        .IgnoreElements()
                        .Concat(Observable.Return(databaseEntities)))
                .Select(databaseEntities => LastUpdated(since, databaseEntities))
                .Select(lastUpdated => UpdateSinceParameters(since, lastUpdated))
                .Do(sinceParameterRepository.Set)
                .Select(sinceParameters => new FetchObservables(fetch, sinceParameters))
                .Select(FinishedPersisting.Transition);
        }

        protected abstract long GetId(TDatabaseInterface entity);

        protected abstract IObservable<IEnumerable<TInterface>> FetchObservable(FetchObservables fetch);

        protected abstract TDatabaseInterface ConvertToDatabaseEntity(TInterface entity);

        protected abstract DateTimeOffset? LastUpdated(ISinceParameters old, IEnumerable<TDatabaseInterface> entities);

        protected abstract ISinceParameters UpdateSinceParameters(ISinceParameters old, DateTimeOffset? lastUpdated);
    }
}
