using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Toggl.Multivac;
using Toggl.Multivac.Models;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Models;

namespace Toggl.Foundation.Suggestions
{
    public sealed class MostUsedTimeEntrySuggestionProvider : ISuggestionProvider
    {
        private const int daysToQuery = 42;
        private const int maxNumberOfSuggestions = 5;
        private readonly TimeSpan thresholdPeriod = TimeSpan.FromDays(daysToQuery);

        private readonly ITogglDatabase database;
        private readonly ITimeService timeService;

        public MostUsedTimeEntrySuggestionProvider(ITogglDatabase database, ITimeService timeService)
        {
            Ensure.Argument.IsNotNull(database, nameof(database));
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));

            this.database = database;
            this.timeService = timeService;
        }

        public IObservable<Suggestion> GetSuggestions()
            => database.TimeEntries
                       .GetAll(isWithinThreshold)
                       .Select(timeEntries => timeEntries.Where(hasDescriptionOrProject))
                       .SelectMany(mostUsedTimeEntry)
                       .Take(maxNumberOfSuggestions);

        private bool hasDescriptionOrProject(ITimeEntry timeEntry)
            => !string.IsNullOrEmpty(timeEntry.Description) || timeEntry.ProjectId != null;

        private bool isWithinThreshold(ITimeEntry timeEntry)
        {
            var delta = timeService.CurrentDateTime - timeEntry.Start;
            return delta <= thresholdPeriod;
        }

        private IEnumerable<Suggestion> mostUsedTimeEntry(IEnumerable<IDatabaseTimeEntry> timeEntries)
            => timeEntries.GroupBy(te => new { te.Description, te.ProjectId, te.TaskId })
                       .OrderByDescending(g => g.Count())
                       .Select(grouping => grouping.First())
                       .Select(timeEntry => new Suggestion(timeEntry));
    }
}
