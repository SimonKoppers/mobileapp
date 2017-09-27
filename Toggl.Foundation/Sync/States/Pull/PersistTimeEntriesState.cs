using System;
using System.Collections.Generic;
using System.Linq;
using Toggl.Foundation.Models;
using Toggl.Foundation.Sync.ConflictResolution;
using Toggl.Multivac.Models;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Models;

namespace Toggl.Foundation.Sync.States
{
    internal sealed class PersistTimeEntriesState : BasePersistState<ITimeEntry, IDatabaseTimeEntry>
    {
        public PersistTimeEntriesState(IRepository<IDatabaseTimeEntry> repository, ISinceParameterRepository sinceParameterRepository, ITimeService timeService)
            : base(repository, sinceParameterRepository, Resolver.ForTimeEntries(), new TimeEntryRivalsResolver(timeService, repository))
        {
        }

        protected override long GetId(IDatabaseTimeEntry entity) => entity.Id;

        protected override IObservable<IEnumerable<ITimeEntry>> FetchObservable(FetchObservables fetch)
            => fetch.TimeEntries;

        protected override IDatabaseTimeEntry ConvertToDatabaseEntity(ITimeEntry entity)
            => TimeEntry.Clean(entity);

        protected override DateTimeOffset? LastUpdated(ISinceParameters old, IEnumerable<IDatabaseTimeEntry> entities)
            => entities.Select(p => p?.At).Where(d => d.HasValue).DefaultIfEmpty(old.TimeEntries).Max();

        protected override ISinceParameters UpdateSinceParameters(ISinceParameters old, DateTimeOffset? lastUpdated)
            => new SinceParameters(old, timeEntries: lastUpdated);
    }
}
