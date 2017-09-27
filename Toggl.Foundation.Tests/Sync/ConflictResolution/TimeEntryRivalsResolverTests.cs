using System;
using System.Linq;
using System.Reactive.Linq;
using FluentAssertions;
using FsCheck.Xunit;
using NSubstitute;
using Toggl.Foundation.Models;
using Toggl.Foundation.Sync.ConflictResolution;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Models;
using Xunit;

namespace Toggl.Foundation.Tests.Sync.ConflictResolution
{
    public sealed class TimeEntryRivalsResolverTests
    {
        private readonly TimeEntryRivalsResolver resolver;

        private readonly ITimeService timeService;

        private static readonly DateTimeOffset arbitraryTime = new DateTimeOffset(2017, 9, 1, 12, 0, 0, TimeSpan.Zero);

        private readonly IQueryable<IDatabaseTimeEntry> timeEntries = new EnumerableQuery<IDatabaseTimeEntry>(new[]
        {
            TimeEntry.Clean(new Ultrawave.Models.TimeEntry { Id = 10, Start = arbitraryTime, Stop = arbitraryTime.AddHours(2) }),
            TimeEntry.Clean(new Ultrawave.Models.TimeEntry { Id = 11, Start = arbitraryTime.AddDays(5), Stop = arbitraryTime.AddDays(6) }),
            TimeEntry.Clean(new Ultrawave.Models.TimeEntry { Id = 12, Start = arbitraryTime.AddDays(10), Stop = arbitraryTime.AddDays(10).AddHours(1) }),
            TimeEntry.Clean(new Ultrawave.Models.TimeEntry { Id = 13, Start = arbitraryTime.AddDays(15), Stop = arbitraryTime.AddDays(15).AddSeconds(13) })
        });

        public TimeEntryRivalsResolverTests()
        {
            timeService = Substitute.For<ITimeService>();
            var repository = Substitute.For<IRepository<IDatabaseTimeEntry>>();
            repository.GetAll(Arg.Any<Func<IDatabaseTimeEntry, bool>>()).Returns(args => Observable.Return(timeEntries.Where((Func<IDatabaseTimeEntry, bool>)args[0])));
            resolver = new TimeEntryRivalsResolver(timeService, repository);
        }

        [Fact]
        public void TimeEntryWhichHasStopTimeSetToNullCanHaveRivals()
        {
            var a = TimeEntry.Clean(new Ultrawave.Models.TimeEntry { Id = 1, Stop = null });

            var canHaveRival = resolver.CanHaveRival(a);

            canHaveRival.Should().BeTrue();
        }

        [Property]
        public void TimeEntryWhichHasStopTimeSetToAnythingElseThanNullCannotHaveRivals(DateTimeOffset stop)
        {
            var a = TimeEntry.Clean(new Ultrawave.Models.TimeEntry { Id = 1, Stop = stop });

            var canHaveRival = resolver.CanHaveRival(a);

            canHaveRival.Should().BeFalse();
        }

        [Fact]
        public void TwoTimeEntriesAreRivalsIfBothOfThemHaveTheStopTimeSetToNull()
        {
            var a = TimeEntry.Clean(new Ultrawave.Models.TimeEntry { Id = 1, Stop = null });
            var b = TimeEntry.Clean(new Ultrawave.Models.TimeEntry { Id = 2, Stop = null });

            var areRivals = resolver.AreRivals(a).Compile()(b);

            areRivals.Should().BeTrue();
        }

        [Property]
        public void TwoTimeEntriesAreNotRivalsIfSomeOfThemHasTheStopTimeNotSetToNull(DateTimeOffset? a, DateTimeOffset? b)
        {
            if (!a.HasValue && !b.HasValue)
                return;

            var x = TimeEntry.Clean(new Ultrawave.Models.TimeEntry { Id = 1, Stop = a });
            var y = TimeEntry.Clean(new Ultrawave.Models.TimeEntry { Id = 2, Stop = b });
            var areRivals = resolver.AreRivals(x).Compile()(y);

            areRivals.Should().BeFalse();
        }

        [Property]
        public void TheTimeEntryWhichHasBeenEditedTheLastWillBeRunningAndTheOtherWillBeStoppedAfterResolution(DateTimeOffset firstAt, DateTimeOffset secondAt)
        {
            (DateTimeOffset earlier, DateTimeOffset later) =
                firstAt < secondAt ? (firstAt, secondAt) : (secondAt, firstAt);
            var a = TimeEntry.Clean(new Ultrawave.Models.TimeEntry { Id = 1, Stop = null, At = earlier });
            var b = TimeEntry.Clean(new Ultrawave.Models.TimeEntry { Id = 2, Stop = null, At = later });

            var (fixedEntityA, fixedRivalB) = resolver.FixRivals(a, b);
            var (fixedEntityB, fixedRivalA) = resolver.FixRivals(b, a);

            fixedEntityA.Stop.Should().NotBeNull();
            fixedRivalA.Stop.Should().NotBeNull();
            fixedRivalB.Stop.Should().BeNull();
            fixedEntityB.Stop.Should().BeNull();
        }

        [Fact]
        public void TheStoppedTimeEntryMustBeMarkedAsSyncNeededAndTheStatusOfTheOtherOneShouldNotChange()
        {
            var a = TimeEntry.Clean(new Ultrawave.Models.TimeEntry { Id = 1, Stop = null, At = arbitraryTime.AddDays(10) });
            var b = TimeEntry.Clean(new Ultrawave.Models.TimeEntry { Id = 2, Stop = null, At = arbitraryTime.AddDays(11) });

            var (fixedA, fixedB) = resolver.FixRivals(a, b);

            fixedA.SyncStatus.Should().Be(SyncStatus.SyncNeeded);
            fixedB.SyncStatus.Should().Be(SyncStatus.InSync);
        }

        [Fact]
        public void TheStoppedEntityMustHaveTheStopTimeEqualToTheStartTimeOfTheNextEntryInTheDatabase()
        {
            var a = TimeEntry.Clean(new Ultrawave.Models.TimeEntry { Id = 1, Stop = null, At = arbitraryTime.AddDays(10), Start = arbitraryTime.AddDays(12) });
            var b = TimeEntry.Clean(new Ultrawave.Models.TimeEntry { Id = 2, Stop = null, At = arbitraryTime.AddDays(11), Start = arbitraryTime.AddDays(13) });

            var (fixedA, _) = resolver.FixRivals(a, b);

            fixedA.Stop.Should().Be(arbitraryTime.AddDays(15));
        }

        [Fact]
        public void TheStoppedEntityMustHaveTheStopTimeEqualToTheCurrentDateTimeOfTheTimeServiceWhenThereIsNoNextEntryInTheDatabase()
        {
            var now = arbitraryTime.AddDays(25);
            timeService.CurrentDateTime.Returns(now);
            var a = TimeEntry.Clean(new Ultrawave.Models.TimeEntry { Id = 1, Stop = null, At = arbitraryTime.AddDays(21), Start = arbitraryTime.AddDays(20) });
            var b = TimeEntry.Clean(new Ultrawave.Models.TimeEntry { Id = 2, Stop = null, At = arbitraryTime.AddDays(22), Start = arbitraryTime.AddDays(21) });

            var (fixedA, _) = resolver.FixRivals(a, b);

            fixedA.Stop.Should().Be(now);
        }
    }
}
