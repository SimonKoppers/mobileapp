using Toggl.Foundation.Sync;
using Toggl.Foundation.Sync.States;
using Toggl.PrimeRadiant;
using Toggl.Ultrawave;
using System.Reactive.Concurrency;
using Toggl.Foundation.Tests.Sync.States;
using Toggl.Multivac.Models;
using Toggl.PrimeRadiant.Models;
using Toggl.Foundation.DataSources;

namespace Toggl.Foundation
{
    public static class TogglSyncManager
    {
        public static ISyncManager CreateSyncManager(
            ITogglDatabase database,
            ITogglApi api,
            ITogglDataSource dataSource,
            ITimeService timeService,
            IScheduler scheduler)
        {
            var queue = new SyncStateQueue();
            var entryPoints = new StateMachineEntryPoints();
            var transitions = new TransitionHandlerProvider();
            ConfigureTransitions(transitions, database, api, dataSource, timeService, entryPoints);
            var stateMachine = new StateMachine(transitions, scheduler);
            var orchestrator = new StateMachineOrchestrator(stateMachine, entryPoints);

            return new SyncManager(queue, orchestrator);
        }

        public static void ConfigureTransitions(
            TransitionHandlerProvider transitions,
            ITogglDatabase database,
            ITogglApi api,
            ITogglDataSource dataSource,
            ITimeService timeService,
            StateMachineEntryPoints entryPoints)
        {
            configurePullTransitions(transitions, database, api, dataSource, timeService, entryPoints.StartPullSync);
            configurePushTransitions(transitions, database, api, dataSource, entryPoints.StartPushSync);
        }

        private static void configurePullTransitions(
            TransitionHandlerProvider transitions,
            ITogglDatabase database,
            ITogglApi api,
            ITogglDataSource dataSource,
            ITimeService timeService,
            StateResult entryPoint)
        {
            var fetchAllSince = new FetchAllSinceState(database, api, timeService);
            var persistWorkspaces = new PersistWorkspacesState(database.Workspaces, database.SinceParameters);
            var persistWorkspaceFeatures = new PersistWorkspacesFeaturesState(database.WorkspaceFeatures, database.SinceParameters);
            var persistTags = new PersistTagsState(database.Tags, database.SinceParameters);
            var persistClients = new PersistClientsState(database.Clients, database.SinceParameters);
            var persistProjects = new PersistProjectsState(database.Projects, database.SinceParameters);
            var persistTimeEntries = new PersistTimeEntriesState(dataSource.TimeEntries, database.SinceParameters, timeService);
            var persistTasks = new PersistTasksState(database.Tasks, database.SinceParameters);

            transitions.ConfigureTransition(entryPoint, fetchAllSince.Start);
            transitions.ConfigureTransition(fetchAllSince.FetchStarted, persistWorkspaces.Start);
            transitions.ConfigureTransition(persistWorkspaces.FinishedPersisting, persistWorkspaceFeatures.Start);
            transitions.ConfigureTransition(persistWorkspaceFeatures.FinishedPersisting, persistTags.Start);
            transitions.ConfigureTransition(persistTags.FinishedPersisting, persistClients.Start);
            transitions.ConfigureTransition(persistClients.FinishedPersisting, persistProjects.Start);
            transitions.ConfigureTransition(persistProjects.FinishedPersisting, persistTasks.Start);
            transitions.ConfigureTransition(persistTasks.FinishedPersisting, persistTimeEntries.Start);
        }
        
        private static void configurePushTransitions(
            TransitionHandlerProvider transitions,
            ITogglDatabase database,
            ITogglApi api,
            ITogglDataSource dataSource,
            StateResult entryPoint)
        {
            configurePushTransitionsForTimeEntries(transitions, database, api, dataSource, entryPoint);
        }

        private static IStateResult configurePushTransitionsForTimeEntries(
            TransitionHandlerProvider transitions,
            ITogglDatabase database,
            ITogglApi api,
            ITogglDataSource dataSource,
            StateResult entryPoint)
        {
            var push = new PushTimeEntriesState(database);
            var pushOne = new PushOneEntityState<IDatabaseTimeEntry>();
            var create = new CreateTimeEntryState(api, dataSource.TimeEntries);
            var update = new UpdateTimeEntryState(api, dataSource.TimeEntries);
            var unsyncable = new UnsyncableTimeEntryState(dataSource.TimeEntries);

            return configurePush(transitions, entryPoint, push, pushOne, create, update, unsyncable);
        }

        private static IStateResult configurePush<T>(
            TransitionHandlerProvider transitions,
            IStateResult entryPoint,
            BasePushState<T> push,
            PushOneEntityState<T> pushOne,
            BaseCreateEntityState<T> create,
            BaseUpdateEntityState<T> update,
            BaseUnsyncableEntityState<T> markUnsyncable)
            where T : class, IBaseModel, IDatabaseSyncable
        {
            transitions.ConfigureTransition(entryPoint, push.Start);
            transitions.ConfigureTransition(push.PushEntity, pushOne.Start);
            transitions.ConfigureTransition(pushOne.CreateEntity, create.Start);
            transitions.ConfigureTransition(pushOne.UpdateEntity, update.Start);
            transitions.ConfigureTransition(create.CreatingFinished, push.Start);
            transitions.ConfigureTransition(create.CreatingFailed, markUnsyncable.Start);
            transitions.ConfigureTransition(update.UpdatingSucceeded, push.Start);
            transitions.ConfigureTransition(update.UpdatingFailed, markUnsyncable.Start);

            return push.NothingToPush;
        }
    }
}
