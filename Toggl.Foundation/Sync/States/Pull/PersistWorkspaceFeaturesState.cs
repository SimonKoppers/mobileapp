using System;
using System.Collections.Generic;
using Toggl.Foundation.Models;
using Toggl.Foundation.Sync.ConflictResolution;
using Toggl.Multivac.Models;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Models;

namespace Toggl.Foundation.Sync.States
{
    internal sealed class PersistWorkspacesFeaturesState : BasePersistState<IWorkspaceFeatureCollection, IDatabaseWorkspaceFeatureCollection>
    {
        public PersistWorkspacesFeaturesState(IRepository<IDatabaseWorkspaceFeatureCollection> repository, ISinceParameterRepository sinceParameterRepository)
            : base(repository, sinceParameterRepository, Resolver.ForWorkspaceFeatures())
        {
        }

        protected override long GetId(IDatabaseWorkspaceFeatureCollection entity) => entity.WorkspaceId;

        protected override IObservable<IEnumerable<IWorkspaceFeatureCollection>> FetchObservable(FetchObservables fetch)
            => fetch.WorkspaceFeatures;

        protected override IDatabaseWorkspaceFeatureCollection ConvertToDatabaseEntity(IWorkspaceFeatureCollection entity)
            => WorkspaceFeatureCollection.From(entity);

        protected override DateTimeOffset? LastUpdated(ISinceParameters old, IEnumerable<IDatabaseWorkspaceFeatureCollection> entities)
            => null;

        protected override ISinceParameters UpdateSinceParameters(ISinceParameters old, DateTimeOffset? lastUpdated)
            => old;
    }
}
