using System;
using System.Reactive;
using System.Reactive.Concurrency;
using Toggl.Foundation.Autocomplete;
using Toggl.Foundation.Sync;
using Toggl.Multivac;
using Toggl.PrimeRadiant;
using Toggl.Ultrawave;

namespace Toggl.Foundation.DataSources
{
    public sealed class TogglDataSource : ITogglDataSource
    {
        private readonly ITogglDatabase database;

        public TogglDataSource(ITogglDatabase database, ITogglApi api, ITimeService timeService, IScheduler scheduler)
        {
            Ensure.Argument.IsNotNull(api, nameof(api));
            Ensure.Argument.IsNotNull(database, nameof(database));
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));
            Ensure.Argument.IsNotNull(scheduler, nameof(scheduler));

            this.database = database;

            User = new UserDataSource(database.User);
            Tags = new TagsDataSource(database.Tags);
            Projects = new ProjectsDataSource(database.Projects);
            TimeEntries = new TimeEntriesDataSource(database.IdProvider, database.TimeEntries, timeService);

            AutocompleteProvider = new AutocompleteProvider(database);
            SyncManager = TogglSyncManager.CreateSyncManager(database, api, this, timeService, scheduler);
        }

        public IUserSource User { get; }
        public ITagsSource Tags { get; }
        public ITasksSource Tasks => throw new NotImplementedException();
        public IClientsSource Clients => throw new NotImplementedException();
        public IProjectsSource Projects { get; }
        public IWorkspacesSource Workspaces => throw new NotImplementedException();
        public ITimeEntriesSource TimeEntries { get; }

        public ISyncManager SyncManager { get; }
        public IAutocompleteProvider AutocompleteProvider { get; }

        public IObservable<Unit> Logout() => database.Clear();
    }
}
