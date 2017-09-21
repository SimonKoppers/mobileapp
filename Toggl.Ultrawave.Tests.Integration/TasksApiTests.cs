﻿using System;
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
        public sealed class TheCreateMethod : AuthenticatedPostEndpointBaseTests<ITask>
        {
            [Fact]
            public async void CreatingTaskFailsInTheFreePlan()
            {
                var (togglApi, user) = await SetupTestUser();
            
                Action creatingTask = () => createTask(togglApi, PricingPlans.Free).Wait();
    
                creatingTask.ShouldThrow<ForbiddenException>();
            }

            [Theory]
            [InlineData(PricingPlans.Starter)]
            [InlineData(PricingPlans.Premium)]
            [InlineData(PricingPlans.Enterprise)]
            public async void CreatingTaskWorksForAllPricingPlansOtherThanTheFreePlan(PricingPlans plan)
            {
                var (togglApi, user) = await SetupTestUser();
            
                Action creatingTask = () => createTask(togglApi, plan).Wait();
    
                creatingTask.ShouldNotThrow();
            }

            protected override IObservable<ITask> CallEndpointWith(ITogglApi togglApi)
                => createTask(togglApi, PricingPlans.Starter);

            private IObservable<ITask> createTask(ITogglApi togglApi, PricingPlans plan)
            {
                var user = togglApi.User.Get().Wait();
                if (plan != PricingPlans.Free)
                    WorkspaceHelper.SetSubscription(user, user.DefaultWorkspaceId, plan).Wait();
            
                var project = togglApi.Projects
                    .Create(
                        new Project
                        {
                            Name = Guid.NewGuid().ToString(),
                            Active = true,
                            WorkspaceId = user.DefaultWorkspaceId
                        })
                    .Wait();

                return togglApi.Tasks
                    .Create(
                        new Task
                        {
                            WorkspaceId = project.WorkspaceId,
                            ProjectId = project.Id,
                            Name = Guid.NewGuid().ToString()
                        });
            }
        }
    }
}
