using System;
using System.Linq;
using System.Linq.Expressions;
using Toggl.Foundation.Models;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Models;

namespace Toggl.Foundation.Sync.ConflictResolution
{
    internal sealed class TimeEntryRivalsResolver : IRivalsResolver<IDatabaseTimeEntry>
    {
        private ITimeService timeService;

        public TimeEntryRivalsResolver(ITimeService timeService)
        {
            this.timeService = timeService;
        }

        public bool CanHaveRival(IDatabaseTimeEntry entity) => !entity.Stop.HasValue;

        public Expression<Func<IDatabaseTimeEntry, bool>> AreRivals(IDatabaseTimeEntry entity)
            => potentialRival => !entity.Stop.HasValue && !potentialRival.Stop.HasValue;

        public (IDatabaseTimeEntry FixedEntity, IDatabaseTimeEntry FixedRival) FixRivals(IDatabaseTimeEntry entity, IDatabaseTimeEntry rival, IQueryable<IDatabaseTimeEntry> allTimeEntries)
            => rival.At < entity.At ? (entity, stop(rival, allTimeEntries)) : (stop(entity, allTimeEntries), rival);

        private IDatabaseTimeEntry stop(IDatabaseTimeEntry toBeStopped, IQueryable<IDatabaseTimeEntry> allTimeEntries)
        {
            var next = allTimeEntries
                .OrderBy(te => te.Start)
                .FirstOrDefault(other => other.Start > toBeStopped.Start);
            var stop = next?.Start ?? timeService.CurrentDateTime;
            return new TimeEntry(toBeStopped, stop);
        }
    }
}
