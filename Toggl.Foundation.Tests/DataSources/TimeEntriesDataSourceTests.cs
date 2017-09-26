using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using FluentAssertions;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.DTOs;
using Toggl.Foundation.Models;
using Toggl.Foundation.Tests.Generators;
using Toggl.Multivac.Models;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Exceptions;
using Toggl.PrimeRadiant.Models;
using Xunit;
using ThreadingTask = System.Threading.Tasks.Task;

namespace Toggl.Foundation.Tests.DataSources
{
    public sealed class TimeEntriesDataSourceTests
    {
        public abstract class TimeEntryDataSourceTest
        {
            protected const long ProjectId = 10;

            protected const long WorkspaceId = 11;

            protected const long UserId = 12;

            protected const long CurrentRunningId = 13;

            protected ITimeEntriesSource TimeEntriesSource { get; }

            protected TestScheduler TestScheduler { get; } = new TestScheduler();

            protected string ValidDescription { get; } = "Testing software";

            protected DateTimeOffset ValidTime { get; } = DateTimeOffset.Now;

            protected IIdProvider IdProvider { get; } = Substitute.For<IIdProvider>();

            protected IDatabaseTimeEntry DatabaseTimeEntry { get; } =
                TimeEntry.Builder
                    .Create(CurrentRunningId)
                    .SetUserId(UserId)
                    .SetDescription("")
                    .SetWorkspaceId(WorkspaceId)
                    .SetSyncStatus(SyncStatus.InSync)
                    .SetAt(DateTimeOffset.Now.AddDays(-1))
                    .SetStart(DateTimeOffset.Now.AddHours(-2))
                    .Build();

            protected IRepository<IDatabaseTimeEntry> Repository { get; } = Substitute.For<IRepository<IDatabaseTimeEntry>>();

            protected ITimeService TimeService { get; } = Substitute.For<ITimeService>();

            public TimeEntryDataSourceTest()
            {
                TimeEntriesSource = new TimeEntriesDataSource(IdProvider, Repository, TimeService);

                IdProvider.GetNextIdentifier().Returns(-1);
                Repository.GetById(Arg.Is(DatabaseTimeEntry.Id)).Returns(Observable.Return(DatabaseTimeEntry));

                Repository.Create(Arg.Any<IDatabaseTimeEntry>())
                          .Returns(info => Observable.Return(info.Arg<IDatabaseTimeEntry>()));

                Repository.Update(Arg.Any<long>(), Arg.Any<IDatabaseTimeEntry>())
                          .Returns(info => Observable.Return(info.Arg<IDatabaseTimeEntry>()));
            }

            protected StartTimeEntryDTO CreateDto(DateTimeOffset startTime, string description, bool billable,
                long? projectId) => new StartTimeEntryDTO
                {
                    UserId = UserId,
                    WorkspaceId = WorkspaceId,
                    StartTime = startTime,
                    Description = description,
                    Billable = billable,
                    ProjectId = projectId
                };
        }

        public sealed class TheConstructor : TimeEntryDataSourceTest
        {
            [Theory]
            [ClassData(typeof(ThreeParameterConstructorTestData))]
            public void ThrowsIfAnyOfTheArgumentsIsNull(bool useIdProvider, bool useRepository, bool useTimeService)
            {
                var idProvider = useIdProvider ? IdProvider : null;
                var repository = useRepository ? Repository : null;
                var timeService = useTimeService ? TimeService : null;

                Action tryingToConstructWithEmptyParameters =
                    () => new TimeEntriesDataSource(idProvider, repository, timeService);

                tryingToConstructWithEmptyParameters
                    .ShouldThrow<ArgumentNullException>();
            }
        }

        public sealed class TheStartMethod : TimeEntryDataSourceTest
        {
            [Fact]
            public async ThreadingTask CreatesANewTimeEntryInTheDatabase()
            {
                await TimeEntriesSource.Start(CreateDto(ValidTime, ValidDescription, true, ProjectId));

                await Repository.Received().Create(Arg.Any<IDatabaseTimeEntry>());
            }

            [Fact]
            public async ThreadingTask CreatesASyncNeededTimeEntry()
            {
                await TimeEntriesSource.Start(CreateDto(ValidTime, ValidDescription, true, ProjectId));

                await Repository.Received().Create(Arg.Is<IDatabaseTimeEntry>(te => te.SyncStatus == SyncStatus.SyncNeeded));
            }

            [Theory]
            [InlineData(true)]
            [InlineData(false)]
            public async ThreadingTask CreatesATimeEntryWithTheProvidedValueForBillable(bool billable)
            {
                await TimeEntriesSource.Start(CreateDto(ValidTime, ValidDescription, billable, ProjectId));

                await Repository.Received().Create(Arg.Is<IDatabaseTimeEntry>(te => te.Billable == billable));
            }

            [Fact]
            public async ThreadingTask CreatesATimeEntryWithTheProvidedValueForDescription()
            {
                await TimeEntriesSource.Start(CreateDto(ValidTime, ValidDescription, true, ProjectId));

                await Repository.Received().Create(Arg.Is<IDatabaseTimeEntry>(te => te.Description == ValidDescription));
            }

            [Fact]
            public async ThreadingTask CreatesATimeEntryWithTheProvidedValueForStartTime()
            {
                await TimeEntriesSource.Start(CreateDto(ValidTime, ValidDescription, true, ProjectId));

                await Repository.Received().Create(Arg.Is<IDatabaseTimeEntry>(te => te.Start == ValidTime));
            }

            [Fact]
            public async ThreadingTask CreatesATimeEntryWithTheProvidedValueForProjectId()
            {
                await TimeEntriesSource.Start(CreateDto(ValidTime, ValidDescription, true, ProjectId));

                await Repository.Received().Create(Arg.Is<IDatabaseTimeEntry>(te => te.ProjectId == ProjectId));
            }

            [Fact]
            public async ThreadingTask CreatesATimeEntryWithTheProvidedValueForUserId()
            {
                await TimeEntriesSource.Start(CreateDto(ValidTime, ValidDescription, true, ProjectId));

                await Repository.Received().Create(Arg.Is<IDatabaseTimeEntry>(te => te.UserId == UserId));
            }

            [Fact]
            public async ThreadingTask CreatesATimeEntryWithTheProvidedValueForWorkspaceId()
            {
                await TimeEntriesSource.Start(CreateDto(ValidTime, ValidDescription, true, ProjectId));

                await Repository.Received().Create(Arg.Is<IDatabaseTimeEntry>(te => te.WorkspaceId == WorkspaceId));
            }

            [Fact]
            public async ThreadingTask CreatesATimeEntryWithAnIdProvidedByTheIdProvider()
            {
                await TimeEntriesSource.Start(CreateDto(ValidTime, ValidDescription, true, ProjectId));

                await Repository.Received().Create(Arg.Is<IDatabaseTimeEntry>(te => te.Id == -1));
            }

            [Fact]
            public async ThreadingTask SetstheCreatedTimeEntryAsTheCurrentlyRunningTimeEntry()
            {
                var observer = TestScheduler.CreateObserver<ITimeEntry>();
                TimeEntriesSource.CurrentlyRunningTimeEntry.Where(te => te != null).Subscribe(observer);

                await TimeEntriesSource.Start(CreateDto(ValidTime, ValidDescription, true, ProjectId));

                var currentlyRunningTimeEntry = observer.Messages.Single().Value.Value;
                await Repository.Received().Create(Arg.Is<IDatabaseTimeEntry>(te => te.Start == currentlyRunningTimeEntry.Start));
            }

            [Fact]
            public async ThreadingTask EmitsANewEventOnTheTimeEntryCreatedObservable()
            {
                var observer = TestScheduler.CreateObserver<ITimeEntry>();
                TimeEntriesSource.TimeEntryCreated.Subscribe(observer);

                await TimeEntriesSource.Start(CreateDto(ValidTime, ValidDescription, true, ProjectId));

                observer.Messages.Single().Value.Value.Id.Should().Be(-1);
                observer.Messages.Single().Value.Value.Start.Should().Be(ValidTime);
            }
        }

        public sealed class TheGetAllMethod : TimeEntryDataSourceTest
        {
            [Fact]
            public async ThreadingTask NeverReturnsDeletedTimeEntries()
            {
                var result = Enumerable
                    .Range(0, 20)
                    .Select(i =>
                    {
                        var isDeleted = i % 2 == 0;
                        var timeEntry = Substitute.For<IDatabaseTimeEntry>();
                        timeEntry.Id.Returns(i);
                        timeEntry.IsDeleted.Returns(isDeleted);
                        return timeEntry;
                    });
                Repository
                    .GetAll(Arg.Any<Func<IDatabaseTimeEntry, bool>>())
                    .Returns(callInfo =>
                        Observable
                             .Return(result)
                             .Select(x => x.Where(callInfo.Arg<Func<IDatabaseTimeEntry, bool>>())));

                var timeEntries = await TimeEntriesSource.GetAll(x => x.Id > 10);

                timeEntries.Should().HaveCount(5);
            }
        }

        public sealed class TheStopMethod : TimeEntryDataSourceTest
        {
            public TheStopMethod()
            {
                var timeEntries = new List<IDatabaseTimeEntry>
                {
                    DatabaseTimeEntry,
                    DatabaseTimeEntry.With(DateTimeOffset.UtcNow),
                    DatabaseTimeEntry.With(DateTimeOffset.UtcNow),
                    DatabaseTimeEntry.With(DateTimeOffset.UtcNow)
                };

                Repository
                    .GetAll(Arg.Any<Func<IDatabaseTimeEntry, bool>>())
                    .Returns(callInfo =>
                        Observable
                             .Return(timeEntries)
                             .Select(x => x.Where(callInfo.Arg<Func<IDatabaseTimeEntry, bool>>())));
            }

            [Fact]
            public async ThreadingTask UpdatesTheTimeEntrySettingItsStopTime()
            {
                await TimeEntriesSource.Stop(ValidTime); 
                await Repository.Received().Update(Arg.Any<long>(), Arg.Is<IDatabaseTimeEntry>(te => te.Stop == ValidTime));
            }

            [Fact]
            public async ThreadingTask UpdatesTheTimeEntryMakingItSyncNeeded()
            {
                await TimeEntriesSource.Stop(ValidTime);

                await Repository.Received().Update(Arg.Any<long>(), Arg.Is<IDatabaseTimeEntry>(te => te.SyncStatus == SyncStatus.SyncNeeded));
            }

            [Fact]
            public async ThreadingTask SetsTheCurrentlyRunningTimeEntryToNull()
            {
                var observer = TestScheduler.CreateObserver<ITimeEntry>();
                TimeEntriesSource.CurrentlyRunningTimeEntry.Subscribe(observer);
                Repository.Update(Arg.Any<long>(), Arg.Any<IDatabaseTimeEntry>()).Returns(callInfo => Observable.Return(callInfo.Arg<IDatabaseTimeEntry>()));

                await TimeEntriesSource.Stop(ValidTime);

                observer.Messages.Single().Value.Value.Should().BeNull();
            }

            [Fact]
            public void ThrowsIfThereAreNoRunningTimeEntries()
            {
                var timeEntries = new List<IDatabaseTimeEntry>
                {
                    DatabaseTimeEntry.With(DateTimeOffset.UtcNow),
                    DatabaseTimeEntry.With(DateTimeOffset.UtcNow),
                    DatabaseTimeEntry.With(DateTimeOffset.UtcNow)
                };

                Repository
                    .GetAll(Arg.Any<Func<IDatabaseTimeEntry, bool>>())
                    .Returns(callInfo =>
                        Observable
                             .Return(timeEntries)
                             .Select(x => x.Where(callInfo.Arg<Func<IDatabaseTimeEntry, bool>>())));

                var observer = TestScheduler.CreateObserver<ITimeEntry>();
                var observable = TimeEntriesSource.Stop(ValidTime);
                observable.Subscribe(observer);

                observer.Messages.Single().Value.Exception.Should().BeOfType<InvalidOperationException>();
            }

            [Fact]
            public async ThreadingTask EmitsANewEventOnTheTimeEntryUpdatedObservable()
            {
                var observer = TestScheduler.CreateObserver<ITimeEntry>();
                TimeEntriesSource.TimeEntryUpdated.Subscribe(observer);

                await TimeEntriesSource.Stop(ValidTime);

                observer.Messages.Single().Value.Value.Id.Should().Be(CurrentRunningId);
                observer.Messages.Single().Value.Value.Stop.Should().Be(ValidTime);
            }
        } 
        public sealed class TheDeleteMethod : TimeEntryDataSourceTest
        {
            [Fact]
            public async ThreadingTask SetsTheDeletedFlag()
            {
                await TimeEntriesSource.Delete(DatabaseTimeEntry.Id).LastOrDefaultAsync();

                await Repository.Received().Update(Arg.Is(DatabaseTimeEntry.Id), Arg.Is<IDatabaseTimeEntry>(te => te.IsDeleted == true));
            }

            [Fact]
            public async ThreadingTask SetsTheSyncNeededStatus()
            {
                await TimeEntriesSource.Delete(DatabaseTimeEntry.Id).LastOrDefaultAsync();

                await Repository.Received().Update(Arg.Is(DatabaseTimeEntry.Id), Arg.Is<IDatabaseTimeEntry>(te => te.SyncStatus == SyncStatus.SyncNeeded));
            }

            [Fact]
            public async ThreadingTask UpdatesTheCorrectTimeEntry()
            {
                await TimeEntriesSource.Delete(DatabaseTimeEntry.Id).LastOrDefaultAsync();

                await Repository.Received().GetById(Arg.Is(DatabaseTimeEntry.Id));
                await Repository.Received().Update(Arg.Is(DatabaseTimeEntry.Id), Arg.Is<IDatabaseTimeEntry>(te => te.Id == DatabaseTimeEntry.Id));
            }

            [Fact]
            public void DoesNotEmitAnyElements()
            {
                var observer = Substitute.For<IObserver<Unit>>();

                TimeEntriesSource.Delete(DatabaseTimeEntry.Id).Subscribe(observer);

                observer.DidNotReceive().OnNext(Arg.Any<Unit>());
                observer.Received().OnCompleted();
            }

            [Fact]
            public void PropagatesErrorIfUpdateFails()
            {
                var timeEntry = TimeEntry.Builder.Create(12)
                      .SetStart(DateTimeOffset.Now)
                      .SetSyncStatus(SyncStatus.InSync)
                      .SetDescription("")
                      .SetUserId(11)
                      .SetWorkspaceId(10)
                      .SetAt(DateTimeOffset.Now)
                      .Build();

                var timeEntryObservable = Observable.Return(timeEntry);
                var errorObservable = Observable.Throw<IDatabaseTimeEntry>(new EntityNotFoundException());
                Repository.GetById(Arg.Is(timeEntry.Id)).Returns(timeEntryObservable);
                Repository.Update(Arg.Any<long>(), Arg.Any<IDatabaseTimeEntry>()).Returns(errorObservable);
                var observer = Substitute.For<IObserver<Unit>>();

                TimeEntriesSource.Delete(timeEntry.Id).Subscribe(observer);

                observer.Received().OnError(Arg.Any<EntityNotFoundException>());
            }

            [Fact]
            public void PropagatesErrorIfTimeEntryIsNotInRepository()
            {
                var observer = Substitute.For<IObserver<Unit>>();
                Repository.GetById(Arg.Any<long>())
                          .Returns(Observable.Throw<IDatabaseTimeEntry>(new EntityNotFoundException()));

                TimeEntriesSource.Delete(12).Subscribe(observer);

                observer.Received().OnError(Arg.Any<EntityNotFoundException>());
            }
        }

        public sealed class TheUpdateMethod : TimeEntryDataSourceTest
        {
            private EditTimeEntryDto prepareTest()
            {
                var observable = Observable.Return(DatabaseTimeEntry);
                Repository.GetById(Arg.Is(DatabaseTimeEntry.Id)).Returns(observable);
                return new EditTimeEntryDto 
                { 
                    Id = DatabaseTimeEntry.Id, 
                    Description = "New description", 
                    StartTime = DateTimeOffset.UtcNow,
                    ProjectId = 13
                };
            }

            private bool ensurePropertiesDidNotChange(IDatabaseTimeEntry timeEntry)
                => timeEntry.Id == DatabaseTimeEntry.Id
                && timeEntry.UserId == DatabaseTimeEntry.UserId
                && timeEntry.IsDeleted == DatabaseTimeEntry.IsDeleted
                && timeEntry.CreatedWith == DatabaseTimeEntry.CreatedWith
                && timeEntry.WorkspaceId == DatabaseTimeEntry.WorkspaceId
                && timeEntry.ServerDeletedAt == DatabaseTimeEntry.ServerDeletedAt;

            [Fact]
            public async ThreadingTask UpdatesTheDescriptionProperty()
            {
                var dto = prepareTest();

                await TimeEntriesSource.Update(dto);

                await Repository.Received().Update(Arg.Is(dto.Id), Arg.Is<IDatabaseTimeEntry>(te => te.Description == dto.Description));
            }

            [Fact]
            public async ThreadingTask UpdatesTheSyncStatusProperty()
            {
                var dto = prepareTest();

                await TimeEntriesSource.Update(dto);

                await Repository.Received().Update(Arg.Is(dto.Id), Arg.Is<IDatabaseTimeEntry>(te => te.SyncStatus == SyncStatus.SyncNeeded));
            }

            [Fact]
            public async ThreadingTask UpdatesTheAtProperty()
            {
                var dto = prepareTest();
                TimeService.CurrentDateTime.Returns(DateTimeOffset.Now);

                await TimeEntriesSource.Update(dto);

                await Repository.Received().Update(Arg.Is(dto.Id), Arg.Is<IDatabaseTimeEntry>(te => te.At > DatabaseTimeEntry.At));
            }

            [Fact]
            public async ThreadingTask UpdatesTheProjectId()
            {
                var dto = prepareTest();

                await TimeEntriesSource.Update(dto);

                await Repository.Received().Update(Arg.Is(dto.Id), Arg.Is<IDatabaseTimeEntry>(te => te.ProjectId == dto.ProjectId));
            }

            [Fact]
            public async ThreadingTask LeavesAllOtherPropertiesUnchanged()
            {
                var dto = prepareTest();

                await TimeEntriesSource.Update(dto);

                await Repository.Received().Update(Arg.Is(dto.Id), Arg.Is<IDatabaseTimeEntry>(te => ensurePropertiesDidNotChange(te)));
            }

            [Fact]
            public async ThreadingTask NotifiesAboutTheUpdate()
            {
                var observable = Observable.Return(DatabaseTimeEntry);
                Repository.GetById(Arg.Is(DatabaseTimeEntry.Id)).Returns(observable);
                var dto = new EditTimeEntryDto { Id = DatabaseTimeEntry.Id, Description = "New description", StartTime = DateTimeOffset.UtcNow };
                var observer = Substitute.For<IObserver<IDatabaseTimeEntry>>();
                TimeEntriesSource.TimeEntryUpdated.Subscribe(observer);

                await TimeEntriesSource.Update(dto);

                observer.Received().OnNext(Arg.Is<IDatabaseTimeEntry>(te => te.Id == dto.Id));
            }
        }
    }
}
