using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FsCheck;
using NSubstitute;
using Toggl.Foundation.Suggestions;
using Toggl.Foundation.Tests.Generators;
using Toggl.Multivac.Models;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Models;
using Xunit;
using TimeEntry = Toggl.Foundation.Models.TimeEntry;

namespace Toggl.Foundation.Tests.Suggestions
{
    public sealed class MostUsedTimeEntrySuggestionProviderTests
    {
        public abstract class MostUsedTimeEntrySuggestionProviderTest
        {
            protected MostUsedTimeEntrySuggestionProvider Provider { get; }
            protected ITimeService TimeService { get; } = Substitute.For<ITimeService>();
            protected ITogglDatabase Database { get; } = Substitute.For<ITogglDatabase>();
            
            protected MostUsedTimeEntrySuggestionProviderTest()
            {
                Provider = new MostUsedTimeEntrySuggestionProvider(Database, TimeService);

                TimeService.CurrentDateTime.Returns(_ => DateTimeOffset.Now);
            }
        }

        public sealed class TheConstructor : MostUsedTimeEntrySuggestionProviderTest
        {
            [Theory]
            [ClassData(typeof(TwoParameterConstructorTestData))]
            public void ThrowsIfAnyOfTheArgumentsIsNull(bool useDatabase, bool useTimeService)
            {
                var database = useDatabase ? Database : null;
                var timeService = useTimeService ? TimeService : null;

                Action tryingToConstructWithEmptyParameters =
                    () => new MostUsedTimeEntrySuggestionProvider(database, timeService);

                tryingToConstructWithEmptyParameters
                    .ShouldThrow<ArgumentNullException>();
            }
        }

        public sealed class TheGetSuggestionsMethod : MostUsedTimeEntrySuggestionProviderTest
        {
            private IEnumerable<IDatabaseTimeEntry> getTimeEntries(params int[] numberOfRepetitions)
            {
                var builder = TimeEntry.Builder.Create(21)
                    .SetUserId(10)
                    .SetWorkspaceId(12)
                    .SetAt(DateTimeOffset.Now)
                    .SetStart(DateTimeOffset.UtcNow);

                return Enumerable.Range(0, numberOfRepetitions.Length)
                    .SelectMany(index => Enumerable
                        .Range(0, numberOfRepetitions[index])
                        .Select(_ => builder
                            .SetDescription($"te{index}")
                            .Build()));
            }

            [Fact]
            public async Task ReturnsEmptyObservableIfThereAreNoTimeEntries()
            {
                Database.TimeEntries
                        .GetAll(Arg.Any<Func<IDatabaseTimeEntry, bool>>())
                        .Returns(Observable.Empty<IEnumerable<IDatabaseTimeEntry>>());

                var suggestions = await Provider.GetSuggestions().ToList();

                suggestions.Should().HaveCount(0);
            }

            [Fact]
            public async Task ReturnsUpToFiveSuggestions()
            {
                var timeEntries = getTimeEntries(2, 2, 2, 3, 3, 4, 5);

                Database.TimeEntries
                        .GetAll(Arg.Any<Func<IDatabaseTimeEntry, bool>>())
                        .Returns(Observable.Return(timeEntries));

                var suggestions = await Provider.GetSuggestions().ToList();

                suggestions.Should().HaveCount(5);
            }

            [Fact]
            public async Task SortsTheSuggestionsByUsage()
            {
                var timeEntries = getTimeEntries(5, 3, 2, 5, 4, 4, 5).ToArray();
                var expectedDescriptions = new[] { 0, 3, 4, 5, 6 }.Select(i => $"te{i}");

                Database.TimeEntries
                        .GetAll(Arg.Any<Func<IDatabaseTimeEntry, bool>>())
                        .Returns(Observable.Return(timeEntries));
                
                var suggestions = await Provider.GetSuggestions().ToList();

                suggestions.Should().OnlyContain(suggestion => expectedDescriptions.Contains(suggestion.Description));
            }

            [Fact]
            public async Task DoesNotReturnTimeEntriesWithoutDescriptionAndProject()
            {
                var builder = TimeEntry.Builder.Create(12)
                                       .SetUserId(9)
                                       .SetWorkspaceId(2)
                                       .SetAt(DateTimeOffset.Now)
                                       .SetStart(DateTimeOffset.UtcNow)
                                       .SetDescription("");
                var emptyTimeEntries = Enumerable.Range(20, 0)
                    .Select(_ => builder.Build());
                var timeEntries = new List<IDatabaseTimeEntry>(emptyTimeEntries);
                timeEntries.AddRange(getTimeEntries(1, 2, 3, 4, 5));
                Database.TimeEntries
                        .GetAll(Arg.Any<Func<IDatabaseTimeEntry, bool>>())
                        .Returns(Observable.Return(timeEntries));

                var suggestions = await Provider.GetSuggestions().ToList();

                suggestions.Should().NotContain(
                    suggestion => !string.IsNullOrEmpty(suggestion.Description)
                                  && suggestion.ProjectId != null
                );
            }
        }
    }
}
