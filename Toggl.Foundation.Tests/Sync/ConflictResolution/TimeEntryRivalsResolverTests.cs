using System;
using System.Linq;
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

        private readonly IQueryable<IDatabaseTimeEntry> timeEntries = new EnumerableQuery<IDatabaseTimeEntry>(new[]
        {
            TimeEntry.Clean(new Ultrawave.Models.TimeEntry { Start = new DateTimeOffset(2017, 9, 10, 12, 0, 0, TimeSpan.Zero) }),
            TimeEntry.Clean(new Ultrawave.Models.TimeEntry { Start = new DateTimeOffset(2017, 9, 15, 12, 0, 0, TimeSpan.Zero) }),
            TimeEntry.Clean(new Ultrawave.Models.TimeEntry { Start = new DateTimeOffset(2017, 9, 20, 12, 0, 0, TimeSpan.Zero) }),
            TimeEntry.Clean(new Ultrawave.Models.TimeEntry { Start = new DateTimeOffset(2017, 9, 25, 12, 0, 0, TimeSpan.Zero) })
        });

        public TimeEntryRivalsResolverTests()
        {
            timeService = Substitute.For<ITimeService>();
            resolver = new TimeEntryRivalsResolver(timeService);
        }

        [Fact]
        public void TimeEntryWhichHasStopTimeSetToNullCanHaveRivals()
        {
            var a = TimeEntry.Clean(new Ultrawave.Models.TimeEntry { Stop = null });

            var canHaveRival = resolver.CanHaveRival(a);

            canHaveRival.Should().BeTrue();
        }

        [Property]
        public void TimeEntryWhichHasStopTimeSetToAnythingElseThanNullCannotHaveRivals(DateTimeOffset stop)
        {
            var a = TimeEntry.Clean(new Ultrawave.Models.TimeEntry { Stop = stop });

            var canHaveRival = resolver.CanHaveRival(a);

            canHaveRival.Should().BeFalse();
        }

        [Fact]
        public void TwoTimeEntriesAreRivalsIfBothOfThemHaveTheStopTimeSetToNull()
        {
            var a = TimeEntry.Clean(new Ultrawave.Models.TimeEntry { Stop = null });
            var b = TimeEntry.Clean(new Ultrawave.Models.TimeEntry { Stop = null });

            var areRivals = resolver.AreRivals(a).Compile()(b);

            areRivals.Should().BeTrue();
        }

        [Property]
        public void TwoTimeEntriesAreNotRivalsIfSomeOfThemHasTheStopTimeNotSetToNull(DateTimeOffset? a, DateTimeOffset? b)
        {
            if (!a.HasValue && !b.HasValue)
                return;

            var x = TimeEntry.Clean(new Ultrawave.Models.TimeEntry { Stop = a });
            var y = TimeEntry.Clean(new Ultrawave.Models.TimeEntry { Stop = b });

            var areRivals = resolver.AreRivals(x).Compile()(y);

            areRivals.Should().BeFalse();
        }

        [Property]
        public void TheTimeEntryWhichHasBeenEditedTheLastWillBeRunningAndTheOtherWillBeStoppedAfterResolution(DateTimeOffset firstAt, DateTimeOffset secondAt)
        {
            (DateTimeOffset earlier, DateTimeOffset later) =
                firstAt < secondAt ? (firstAt, secondAt) : (secondAt, firstAt);
            var a = TimeEntry.Clean(new Ultrawave.Models.TimeEntry { Stop = null, At = earlier });
            var b = TimeEntry.Clean(new Ultrawave.Models.TimeEntry { Stop = null, At = later });

            var (fixedEntityA, fixedRivalB) = resolver.FixRivals(a, b, timeEntries);
            var (fixedEntityB, fixedRivalA) = resolver.FixRivals(b, a, timeEntries);

            fixedEntityA.Stop.Should().NotBeNull();
            fixedRivalA.Stop.Should().NotBeNull();
            fixedRivalB.Stop.Should().BeNull();
            fixedEntityB.Stop.Should().BeNull();
        }

        [Fact]
        public void TheStoppedTimeEntryMustBeMarkedAsSyncNeededAndTheStatusOfTheOtherOneShouldNotChange()
        {
            var a = TimeEntry.Clean(new Ultrawave.Models.TimeEntry { Stop = null, At = new DateTimeOffset(2017, 9, 1, 12, 34, 56, TimeSpan.Zero) });
            var b = TimeEntry.Clean(new Ultrawave.Models.TimeEntry { Stop = null, At = new DateTimeOffset(2017, 9, 9, 12, 34, 56, TimeSpan.Zero) });

            var (fixedA, fixedB) = resolver.FixRivals(a, b, timeEntries);

            fixedA.SyncStatus.Should().Be(SyncStatus.SyncNeeded);
            fixedB.SyncStatus.Should().Be(SyncStatus.InSync);
        }

        [Fact]
        public void TheStoppedEntityMustHaveTheStopTimeEqualToTheStartTimeOfTheNextEntryInTheDatabase()
        {
            var a = TimeEntry.Clean(new Ultrawave.Models.TimeEntry { Stop = null, At = new DateTimeOffset(2017, 9, 17, 12, 34, 56, TimeSpan.Zero), Start = new DateTimeOffset(2017, 9, 17, 12, 34, 56, TimeSpan.Zero) });
            var b = TimeEntry.Clean(new Ultrawave.Models.TimeEntry { Stop = null, At = new DateTimeOffset(2017, 9, 19, 12, 34, 56, TimeSpan.Zero) });

            var (fixedA, _) = resolver.FixRivals(a, b, timeEntries);

            fixedA.Stop.Should().Be(new DateTimeOffset(2017, 9, 20, 12, 0, 0, TimeSpan.Zero));
        }

        [Fact]
        public void TheStoppedEntityMustHaveTheStopTimeEqualToTheCurrentDateTimeOfTheTimeServiceWhenThereIsNoNextEntryInTheDatabase()
        {
            var now = new DateTimeOffset(2017, 10, 25, 12, 34, 56, TimeSpan.Zero);
            timeService.CurrentDateTime.Returns(now);
            var a = TimeEntry.Clean(new Ultrawave.Models.TimeEntry { Stop = null, At = new DateTimeOffset(2017, 10, 17, 12, 34, 56, TimeSpan.Zero), Start = new DateTimeOffset(2017, 10, 1, 12, 34, 56, TimeSpan.Zero) });
            var b = TimeEntry.Clean(new Ultrawave.Models.TimeEntry { Stop = null, At = new DateTimeOffset(2017, 10, 19, 12, 34, 56, TimeSpan.Zero) });

            var (fixedA, _) = resolver.FixRivals(a, b, timeEntries);

            fixedA.Stop.Should().Be(now);
        }
    }
}
