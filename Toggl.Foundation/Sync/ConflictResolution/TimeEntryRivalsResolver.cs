using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Linq;
using Toggl.Foundation.Models;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Models;

namespace Toggl.Foundation.Sync.ConflictResolution
{
    internal sealed class TimeEntryRivalsResolver : IRivalsResolver<IDatabaseTimeEntry>
    {
        private ITimeService timeService;
        private IRepository<IDatabaseTimeEntry> timeEntries;

        public TimeEntryRivalsResolver(ITimeService timeService, IRepository<IDatabaseTimeEntry> timeEntries)
        {
            this.timeService = timeService;
            this.timeEntries = timeEntries;
        }

        public bool CanHaveRival(IDatabaseTimeEntry entity) => !entity.Stop.HasValue;

        public Expression<Func<IDatabaseTimeEntry, bool>> AreRivals(IDatabaseTimeEntry entity)
            => CanHaveRival(entity)
                ? potentialRival => potentialRival.Stop == null && potentialRival.Id != entity.Id
                : (Expression<Func<IDatabaseTimeEntry, bool>>)(_ => false);

        public (IDatabaseTimeEntry FixedEntity, IDatabaseTimeEntry FixedRival) FixRivals(IDatabaseTimeEntry entity, IDatabaseTimeEntry rival)
            => rival.At < entity.At ? (entity, stop(rival)) : (stop(entity), rival);

        private IDatabaseTimeEntry stop(IDatabaseTimeEntry toBeStopped)
        {
            var next = timeEntries
                .GetAll(other => other.Start > toBeStopped.Start)
                .Select(all => all.OrderBy(te => te.Start).FirstOrDefault())
                .Wait();
            var stop = next?.Start ?? timeService.CurrentDateTime;
            return new TimeEntry(toBeStopped, stop);
        }
    }
}
