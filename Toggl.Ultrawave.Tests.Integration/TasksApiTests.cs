using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reactive.Linq;
using FluentAssertions;
using Xunit;
using Toggl.Ultrawave.Models;
using Toggl.Ultrawave.Exceptions;
using Toggl.Ultrawave.Tests.Integration.BaseTests;
using Toggl.Multivac.Models;
using Toggl.Ultrawave.Tests.Integration.Helper;

namespace Toggl.Ultrawave.Tests.Integration
{
    public sealed class TasksApiTests
    {
        public sealed class TheGetAllMethod : AuthenticatedEndpointBaseTests<List<ITask>>
        {
            protected override IObservable<List<ITask>> CallEndpointWith(ITogglApi togglApi)
            {
                var user = togglApi.User.Get().Wait();
                WorkspaceHelper.SetSubscription(user, user.DefaultWorkspaceId, PricingPlans.Starter).Wait();
                return togglApi.Tasks.GetAll();
            }

            [Fact, LogTestInfo]
            public async System.Threading.Tasks.Task ReturnsAllTasks()
            {
                var (togglClient, user) = await SetupTestUser();
                var project = await createProject(togglClient, user.DefaultWorkspaceId);
                WorkspaceHelper.SetSubscription(user, user.DefaultWorkspaceId, PricingPlans.Starter).Wait();
                var taskA = await togglClient.Tasks.Create(randomTask(project, user.Id));
                var taskB = await togglClient.Tasks.Create(randomTask(project, user.Id));

                var tasks = await togglClient.Tasks.GetAll();

                tasks.Should().HaveCount(2);
                tasks.Should().Contain(x => isTheSameAs(taskA, x));
                tasks.Should().Contain(x => isTheSameAs(taskB, x));
            }

            [Fact, LogTestInfo]
            public async System.Threading.Tasks.Task ReturnsOnlyActiveProjects()
            {
                var (togglClient, user) = await SetupTestUser();
                var project = await createProject(togglClient, user.DefaultWorkspaceId);
                WorkspaceHelper.SetSubscription(user, user.DefaultWorkspaceId, PricingPlans.Starter).Wait();
                var task = await togglClient.Tasks.Create(randomTask(project, user.Id));
                var inactiveTask = await togglClient.Tasks.Create(randomTask(project, user.Id, isActive: false));

                var tasks = await togglClient.Tasks.GetAll();

                tasks.Should().HaveCount(1);
                tasks.Should().Contain(x => isTheSameAs(x, task));
                tasks.Should().NotContain(x => isTheSameAs(x, inactiveTask));
            }

            [Fact, LogTestInfo]
            public async System.Threading.Tasks.Task ReturnsEmptyListWhenThereAreNoActiveProjects()
            {
                var (togglClient, user) = await SetupTestUser();
                var project = await createProject(togglClient, user.DefaultWorkspaceId);
                WorkspaceHelper.SetSubscription(user, user.DefaultWorkspaceId, PricingPlans.Starter).Wait();
                await togglClient.Tasks.Create(randomTask(project, user.Id, isActive: false));

                var tasks = await togglClient.Tasks.GetAll();

                tasks.Should().HaveCount(0);
            }
        }

        public sealed class TheGetAllSinceMethod : AuthenticatedGetSinceEndpointBaseTests<ITask>
        {
            private IProject project;

            protected override IObservable<List<ITask>> CallEndpointWith(ITogglApi togglApi, DateTimeOffset threshold)
            {
                var user = togglApi.User.Get().Wait();
                WorkspaceHelper.SetSubscription(user, user.DefaultWorkspaceId, PricingPlans.Starter).Wait();
                return togglApi.Tasks.GetAllSince(threshold);
            }

            protected override DateTimeOffset AtDateOf(ITask model)
                => model.At;

            protected override ITask MakeUniqueModel(ITogglApi api, IUser user)
                => new Task { Active = true, Name = Guid.NewGuid().ToString(), WorkspaceId = user.DefaultWorkspaceId, ProjectId = getProject(api, user.DefaultWorkspaceId).Id };

            protected override IObservable<ITask> PostModelToApi(ITogglApi api, ITask model)
            {
                var user = api.User.Get().Wait();
                WorkspaceHelper.SetSubscription(user, user.DefaultWorkspaceId, PricingPlans.Starter).Wait();
                return api.Tasks.Create(model);
            }

            protected override Expression<Func<ITask, bool>> ModelWithSameAttributesAs(ITask model)
                => p => isTheSameAs(model, p);

            private IProject getProject(ITogglApi api, long workspaceId)
            {
                if (project == null)
                    project = createProject(api, workspaceId).Wait();

                return project;
            }
        }


        public sealed class TheCreateMethod : AuthenticatedPostEndpointBaseTests<ITask>
        {
            [Fact]
            public async void CreatingTaskFailsInTheFreePlan()
            {
                var (togglApi, user) = await SetupTestUser();
                var project = await createProject(togglApi, user.DefaultWorkspaceId);

                Action creatingTask = () => createTask(togglApi, project, user.Id).Wait();

                creatingTask.ShouldThrow<ForbiddenException>();
            }

            [Theory]
            [InlineData(PricingPlans.Starter)]
            [InlineData(PricingPlans.Premium)]
            [InlineData(PricingPlans.Enterprise)]
            public async void CreatingTaskWorksForAllPricingPlansOtherThanTheFreePlan(PricingPlans plan)
            {
                var (togglApi, user) = await SetupTestUser();
                WorkspaceHelper.SetSubscription(user, user.DefaultWorkspaceId, plan).Wait();
                var project = createProject(togglApi, user.DefaultWorkspaceId).Wait();

                Action creatingTask = () => createTask(togglApi, project, user.Id).Wait();

                creatingTask.ShouldNotThrow();
            }

            protected override IObservable<ITask> CallEndpointWith(ITogglApi togglApi)
            {
                var user = togglApi.User.Get().Wait();
                WorkspaceHelper.SetSubscription(user, user.DefaultWorkspaceId, PricingPlans.Starter).Wait();
                var project = createProject(togglApi, user.DefaultWorkspaceId).Wait();
                return createTask(togglApi, project, user.Id);
            }
        }

        private static ITask randomTask(IProject project, long userId, bool isActive = true)
            => new Task
            {
                WorkspaceId = project.WorkspaceId,
                ProjectId = project.Id,
                UserId = userId,
                Name = Guid.NewGuid().ToString(),
                Active = isActive,
            };

        private static IProject randomProject(long workspaceId)
            => new Project
            {
                WorkspaceId = workspaceId,
                Name = Guid.NewGuid().ToString(),
                Active = true
            };

        private static IObservable<IProject> createProject(ITogglApi togglApi, long workspaceId)
            => togglApi.Projects.Create(randomProject(workspaceId));

        private static IObservable<ITask> createTask(ITogglApi togglApi, IProject project, long userId)
            => togglApi.Tasks.Create(randomTask(project, userId));

        private static bool isTheSameAs(ITask a, ITask b)
            => a.Id == b.Id
               && a.Name == b.Name
               && a.Active == b.Active
               && a.ProjectId == b.ProjectId
               && a.EstimatedSeconds == b.EstimatedSeconds
               && a.TrackedSeconds == b.TrackedSeconds
               && a.WorkspaceId == b.WorkspaceId
               && a.UserId == b.UserId;
    }
}
