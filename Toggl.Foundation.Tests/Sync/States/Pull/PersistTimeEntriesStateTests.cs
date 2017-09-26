using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using NSubstitute;
using Toggl.Foundation.Models;
using Toggl.Foundation.Sync.States;
using Toggl.Multivac.Models;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Models;
using TimeEntry = Toggl.Ultrawave.Models.TimeEntry;

namespace Toggl.Foundation.Tests.Sync.States
{
    public sealed class PersistTimeEntriesStateTests : PersistStateTests
    {
        public PersistTimeEntriesStateTests()
            : base(new TheStartMethod())
        {
        }

        private sealed class TheStartMethod
            : TheStartMethod<PersistTimeEntriesState, ITimeEntry, IDatabaseTimeEntry>
        {
            private readonly ITimeService timeService;

            public TheStartMethod()
            {
                timeService = Substitute.For<ITimeService>();
                timeService.CurrentDateTime.Returns(_ => DateTimeOffset.Now);
            }

            protected override PersistTimeEntriesState CreateState(IRepository<IDatabaseTimeEntry> repository, ISinceParameterRepository sinceParameterRepository)
                => new PersistTimeEntriesState(repository, sinceParameterRepository, timeService);

            protected override List<ITimeEntry> CreateListWithOneItem(DateTimeOffset? at = null)
                => new List<ITimeEntry> { new TimeEntry { At = at ?? DateTimeOffset.Now, Description = Guid.NewGuid().ToString() } };

            protected override FetchObservables CreateObservablesWhichFetchesTwice()
                => CreateFetchObservables(
                    null, new SinceParameters(null),
                    timeEntries: Observable.Create<List<ITimeEntry>>(observer =>
                    {
                        observer.OnNext(new List<ITimeEntry>());
                        observer.OnNext(new List<ITimeEntry>());
                        return () => { };
                    }));

            protected override bool OtherSinceDatesDidntChange(ISinceParameters old, ISinceParameters next,
                DateTimeOffset at)
                => next.Workspaces == old.Workspaces
                   && next.Projects == old.Projects
                   && next.Clients == old.Clients
                   && next.Tags == old.Tags
                   && next.Tasks == old.Tasks
                   && next.TimeEntries == at;

            protected override FetchObservables CreateObservables(
                ISinceParameters since = null,
                List<ITimeEntry> timeEntries = null)
                => new FetchObservables(
                    since ?? new SinceParameters(null),
                    Observable.Return(new List<IWorkspace>()),
                    Observable.Return(new List<IWorkspaceFeatureCollection>()),
                    Observable.Return(new List<IClient>()),
                    Observable.Return(new List<IProject>()),
                    Observable.Return(timeEntries),
                    Observable.Return(new List<ITag>()),
                    Observable.Return(new List<ITask>()));

            protected override List<ITimeEntry> CreateComplexListWhereTheLastUpdateEntityIsDeleted(DateTimeOffset? maybeAt)
            {
                var at = maybeAt ?? DateTimeOffset.Now;
                return new List<ITimeEntry>
                {
                    new TimeEntry { At = at.AddDays(-1), Description = Guid.NewGuid().ToString() },
                    new TimeEntry { At = at.AddDays(-3), Description = Guid.NewGuid().ToString() },
                    new TimeEntry { At = at, ServerDeletedAt = at, Description = Guid.NewGuid().ToString() },
                    new TimeEntry { At = at.AddDays(-2), Description = Guid.NewGuid().ToString() }
                };
            }

            protected override bool IsDeletedOnServer(ITimeEntry entry) => entry.ServerDeletedAt.HasValue;

            protected override IDatabaseTimeEntry Clean(ITimeEntry entry) => Models.TimeEntry.Clean(entry);

            protected override Func<IDatabaseTimeEntry, bool> ArePersistedAndClean(List<ITimeEntry> entities)
                => persisted => persisted.SyncStatus == SyncStatus.InSync && entities.Any(te => te.Description == persisted.Description);
        }
    }
}
