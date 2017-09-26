using System;
using System.Reactive.Linq;
using Toggl.Foundation.Models;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Models;
using Toggl.Ultrawave;

namespace Toggl.Foundation.Sync.States
{
    internal sealed class CreateTimeEntryState : BaseCreateEntityState<IDatabaseTimeEntry>
    {
        public CreateTimeEntryState(ITogglApi api, IRepository<IDatabaseTimeEntry> repository, IRetryDelayService delay) : base(api, repository, delay)
        {
        }

        protected override IObservable<IDatabaseTimeEntry> Create(ITogglApi api, IDatabaseTimeEntry entity)
            => api.TimeEntries.Create(entity).Select(TimeEntry.Clean);

        protected override IDatabaseTimeEntry CopyFrom(IDatabaseTimeEntry entity)
            => TimeEntry.From(entity);
    }
}
