using System;
using System.Reactive.Linq;
using Toggl.Foundation.Models;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Models;
using Toggl.Ultrawave;

namespace Toggl.Foundation.Sync.States
{
    internal sealed class UpdateTimeEntryState : BaseUpdateEntityState<IDatabaseTimeEntry>
    {
        public UpdateTimeEntryState(ITogglApi api, IRepository<IDatabaseTimeEntry> repository, IRetryDelayService delay) : base(api, repository, delay)
        {
        }

        protected override bool HasChanged(IDatabaseTimeEntry original, IDatabaseTimeEntry current)
            => original.At < current.At;

        protected override IObservable<IDatabaseTimeEntry> Update(ITogglApi api, IDatabaseTimeEntry entity)
            => api.TimeEntries.Update(entity).Select(TimeEntry.Clean);

        protected override IDatabaseTimeEntry CopyFrom(IDatabaseTimeEntry entity)
            => TimeEntry.From(entity);
    }
}
