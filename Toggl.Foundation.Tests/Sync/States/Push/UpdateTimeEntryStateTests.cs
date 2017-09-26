using System;
using NSubstitute;
using Toggl.Foundation.Models;
using Toggl.Foundation.Sync;
using Toggl.Foundation.Sync.States;
using Toggl.Multivac.Models;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Models;
using Toggl.Ultrawave;

namespace Toggl.Foundation.Tests.Sync.States
{
    public sealed class UpdateTimeEntryStateTests : BaseUpdateEntityStateTests
    {
        public UpdateTimeEntryStateTests()
            : base(new TheStartMethod())
        {
        }

        private sealed class TheStartMethod : TheStartMethod<IDatabaseTimeEntry, ITimeEntry>
        {
            protected override BaseUpdateEntityState<IDatabaseTimeEntry> CreateState(ITogglApi api, IRepository<IDatabaseTimeEntry> repository)
                => new UpdateTimeEntryState(api, repository, Substitute.For<IRetryDelayService>());

            protected override Func<IDatabaseTimeEntry, IObservable<ITimeEntry>> GetUpdateFunction(ITogglApi api)
                => api.TimeEntries.Update;

            protected override IDatabaseTimeEntry CreateDirtyEntity(long id, DateTimeOffset lastUpdate = default(DateTimeOffset))
                => TimeEntry.Dirty(new Ultrawave.Models.TimeEntry { Id = id, Description = Guid.NewGuid().ToString(), At = lastUpdate });

            protected override void AssertUpdateReceived(ITogglApi api, IDatabaseTimeEntry entity)
                => api.TimeEntries.Received().Update(entity);
        }
    }
}
