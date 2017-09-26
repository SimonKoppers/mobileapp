using System;
using NSubstitute;
using Toggl.Foundation.Models;
using Toggl.Foundation.Sync;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Models;

namespace Toggl.Foundation.Tests.Sync.States.Push
{
    public sealed class UnsyncableTimeEntryStateTests : BaseUnsyncableEntityStateTests
    {
        public UnsyncableTimeEntryStateTests()
            : base(new TheStartMethod())
        {
        }

        private sealed class TheStartMethod : TheStartMethod<IDatabaseTimeEntry>
        {
            protected override BaseUnsyncableEntityState<IDatabaseTimeEntry> CreateState(IRepository<IDatabaseTimeEntry> repository)
                => new UnsyncableTimeEntryState(repository, Substitute.For<IRetryDelayService>());

            protected override IDatabaseTimeEntry CreateDirtyEntity()
                => TimeEntry.Dirty(new Ultrawave.Models.TimeEntry { Description = Guid.NewGuid().ToString() });
        }
    }
}
