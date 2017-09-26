using System;
using System.Reactive.Linq;
using Toggl.Multivac;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Models;

namespace Toggl.Foundation.DataSources
{
    internal sealed class WorkspacesDataSource : IWorkspacesSource
    {
        private readonly ITogglDatabase database;

        public WorkspacesDataSource(ITogglDatabase database)
        {
            Ensure.Argument.IsNotNull(database, nameof(database));

            this.database = database;
        }

        public IObservable<IDatabaseWorkspace> GetById(long id)
            => database.Workspaces.GetById(id);

        public IObservable<IDatabaseWorkspace> GetDefault()
            => database.User
                .Single()
                .SelectMany(user => GetById(user.DefaultWorkspaceId));

        public IObservable<bool> WorkspaceHasFeature(long workspaceId, WorkspaceFeatureId feature)
            => database.WorkspaceFeatures
                .GetById(workspaceId)
                .Select(featureCollection => featureCollection.IsEnabled(feature));
    }
}
