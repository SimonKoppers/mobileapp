using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using FluentAssertions;
using FsCheck.Xunit;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Toggl.Foundation.Sync;
using Toggl.Foundation.Sync.States.Push;
using Toggl.PrimeRadiant;
using Toggl.Ultrawave;

namespace Toggl.Foundation.Tests.Sync.States.Push
{
    public abstract class BaseRetryPushStateTests
    {
        private ITogglApi api;
        private TestScheduler scheduler;
        private readonly TestModel testEntity = new TestModel(123, SyncStatus.SyncNeeded);

        public BaseRetryPushStateTests()
        {
            api = Substitute.For<ITogglApi>();
            scheduler = new TestScheduler();
        }

        [Property]
        public void TheServerIsAvailableTransitionIsReturnedWhenTheStatusEndpointReturnsOK(int seed)
        {
            var state = CreateState(api, scheduler, new Random(seed));
            var transition = prepareSimpleTransition(state, true, 0).Wait();
            var parameter = ((Transition<TestModel>)transition).Parameter;

            transition.Result.Should().Be(state.ServerIsAvailable);
            parameter.Should().Be(testEntity);
        }

        [Property]
        public void TheNextTransitionIsDelayedAtLeastForSomeSpecificPeriodWhenTheServerIsNotAvailable(int seed, double lastWaitingTime)
        {
            if (double.IsNaN(lastWaitingTime) || lastWaitingTime <= TimeSpan.FromTicks(1).TotalSeconds || lastWaitingTime > TimeSpan.MaxValue.TotalSeconds)
                return;

            var state = CreateState(api, scheduler, new Random(seed));
            var hasCompleted = false;

            var transition = prepareSimpleTransition(state, false, lastWaitingTime);
            var subscription = transition.Subscribe(_ => hasCompleted = true);
            scheduler.AdvanceBy(TimeSpan.FromSeconds(lastWaitingTime * MinProlongingFactor).Ticks);
            subscription.Dispose();

            hasCompleted.Should().BeFalse();
        }

        [Property]
        public void TheNextTransitionIsDelayedAtMostForSomeSpecificPeriodWhenTheServerIsNotAvailable(int seed, double lastWaitingTime)
        {
            if (double.IsNaN(lastWaitingTime) || lastWaitingTime <= TimeSpan.FromTicks(1).TotalSeconds || lastWaitingTime > TimeSpan.MaxValue.TotalSeconds)
                return;

            var state = CreateState(api, scheduler, new Random(seed));
            ITransition transition = null;

            var transitionObservable = prepareSimpleTransition(state, false, lastWaitingTime);
            var subscription = transitionObservable.Subscribe(t => transition = t);
            scheduler.AdvanceBy(TimeSpan.FromSeconds(lastWaitingTime * MaxProlongingFactor).Ticks);
            subscription.Dispose();

            transition.Should().NotBeNull();
            var parameter = ((Transition<(TestModel, double LastWaitingTime)>)transition).Parameter;
            parameter.LastWaitingTime.Should().BeGreaterOrEqualTo(lastWaitingTime * MinProlongingFactor).And.BeLessOrEqualTo(lastWaitingTime * MaxProlongingFactor);
        }

        [Property]
        public void TheNextTransitionIsDelayedAtLeastForTheInitialWaitingPeriodWhenTheServerIsNotAvailableAndItIsTheFirstWaitingPeriod(int seed)
        {
            var state = CreateState(api, scheduler, new Random(seed));
            var hasCompleted = false;

            var transition = prepareSimpleTransition(state, false, 0);
            var subscription = transition.Subscribe(_ => hasCompleted = true);
            scheduler.AdvanceBy(TimeSpan.FromSeconds(DefaultWaitingTime).Ticks - 1);
            subscription.Dispose();

            hasCompleted.Should().BeFalse();
        }

        [Property]
        public void TheNextTransitionIsDelayedAtMostForTheInitialWaitingPeriodWhenTheServerIsNotAvailableAndItIsTheFirstWaitingPeriod(int seed)
        {
            var state = CreateState(api, scheduler, new Random(seed));
            ITransition transition = null;

            var transitionObservable = prepareSimpleTransition(state, false, 0);
            var subscription = transitionObservable.Subscribe(t => transition = t);
            scheduler.AdvanceBy(TimeSpan.FromSeconds(DefaultWaitingTime).Ticks + 1);
            subscription.Dispose();

            transition.Should().NotBeNull();
            var parameter = ((Transition<(TestModel, double LastWaitingTime)>)transition).Parameter;
            parameter.LastWaitingTime.Should().Be(DefaultWaitingTime);
        }

        protected abstract BaseRetryPushState<TestModel> CreateState(ITogglApi api, IScheduler scheduler, Random rnd);

        protected abstract double DefaultWaitingTime { get; }

        protected abstract double MinProlongingFactor { get; }

        protected abstract double MaxProlongingFactor { get; }

        private IObservable<ITransition> prepareSimpleTransition(BaseRetryPushState<TestModel> state, bool serverStatus, double lastWaitingTime)
        {
            api.Status.Get().Returns(Observable.Return(serverStatus));
            return state.Start((testEntity, lastWaitingTime));
        }
    }
}
