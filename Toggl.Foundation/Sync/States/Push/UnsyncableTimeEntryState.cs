using Toggl.Foundation.Models;
using Toggl.Foundation.Sync;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Models;

namespace Toggl.Foundation.Tests.Sync.States
{
    internal sealed class UnsyncableTimeEntryState : BaseUnsyncableEntityState<IDatabaseTimeEntry>
    {
        public UnsyncableTimeEntryState(IRepository<IDatabaseTimeEntry> repository, IRetryDelayService delay) : base(repository, delay)
        {
        }

        protected override bool HasChanged(IDatabaseTimeEntry original, IDatabaseTimeEntry current)
            => original.At < current.At;

        protected override IDatabaseTimeEntry CreateUnsyncableFrom(IDatabaseTimeEntry entity, string reason)
            => TimeEntry.Unsyncable(entity, reason);

        protected override IDatabaseTimeEntry CopyFrom(IDatabaseTimeEntry entity)
            => TimeEntry.From(entity);
    }
}
