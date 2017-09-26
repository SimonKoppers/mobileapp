using System;
using System.Reactive.Linq;
using FluentAssertions;
using FsCheck.Xunit;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Toggl.Foundation.Sync;
using Toggl.Foundation.Sync.States.Push;
using Toggl.Ultrawave;
using Toggl.Ultrawave.Exceptions;
using Xunit;

namespace Toggl.Foundation.Tests.Sync.States.Push
{
    public sealed class CheckServerStatusStateTests
    {
        private ITogglApi api;
        private TestScheduler scheduler;
        private IRetryDelayService delay;
        private readonly CheckServerStatusState state;

        public CheckServerStatusStateTests()
        {
            api = Substitute.For<ITogglApi>();
            scheduler = new TestScheduler();
            delay = Substitute.For<IRetryDelayService>();
            state = new CheckServerStatusState(api, scheduler, delay);
        }

        [Fact]
        public void TheServerIsAvailableTransitionIsReturnedWhenTheStatusEndpointReturnsOK()
        {
            api.Status.Get().Returns(Observable.Return(true));

            var transition = state.Start().Wait();

            transition.Result.Should().Be(state.ServerIsAvailable);
        }

        [Fact]
        public void TheTransitionIsDelayedAtMostByTheNextSlowDelayTimeFromTheRetryDelayServiceWhenInternalServerErrorOccurs()
        {
            api.Status.Get().Returns(Observable.Throw<bool>(new InternalServerErrorException()));
            delay.NextFastDelay().Returns(TimeSpan.FromSeconds(100));
            delay.NextSlowDelay().Returns(TimeSpan.FromSeconds(10));
            var hasCompleted = false;

            var transition = state.Start();
            var subscription = transition.Subscribe(_ => hasCompleted = true);
            scheduler.AdvanceBy(TimeSpan.FromSeconds(10).Ticks);
            subscription.Dispose();

            hasCompleted.Should().BeTrue();
        }

        [Fact]
        public void TheTransitionIsDelayedAtLeastByTheNextSlowDelayTimeFromTheRetryDelayServiceWhenInternalServerErrorOccurs()
        {
            api.Status.Get().Returns(Observable.Throw<bool>(new InternalServerErrorException()));
            delay.NextFastDelay().Returns(TimeSpan.FromSeconds(1));
            delay.NextSlowDelay().Returns(TimeSpan.FromSeconds(10));
            var hasCompleted = false;

            var transition = state.Start();
            var subscription = transition.Subscribe(_ => hasCompleted = true);
            scheduler.AdvanceBy(TimeSpan.FromSeconds(10).Ticks - 1);
            subscription.Dispose();

            hasCompleted.Should().BeFalse();
        }

        [Theory]
        [MemberData(nameof(ServerExceptionsOtherThanInternalServerErrorException))]
        public void TheTransitionIsDelayedAtMostByTheNextFastDelayTimeFromTheRetryDelayServiceWhenAServerErrorOtherThanInternalServerErrorOccurs(ServerErrorException exception)
        {
            api.Status.Get().Returns(Observable.Throw<bool>(exception));
            delay.NextFastDelay().Returns(TimeSpan.FromSeconds(10));
            delay.NextSlowDelay().Returns(TimeSpan.FromSeconds(100));
            var hasCompleted = false;

            var transition = state.Start();
            var subscription = transition.Subscribe(_ => hasCompleted = true);
            scheduler.AdvanceBy(TimeSpan.FromSeconds(10).Ticks + 1);
            subscription.Dispose();

            hasCompleted.Should().BeTrue();
        }

        [Theory]
        [MemberData(nameof(ServerExceptionsOtherThanInternalServerErrorException))]
        public void TheTransitionIsDelayedAtLeastByTheNextFastDelayTimeFromTheRetryDelayServiceWhenAServerErrorOtherThanInternalServerErrorOccurs(ServerErrorException exception)
        {
            api.Status.Get().Returns(Observable.Throw<bool>(exception));
            delay.NextFastDelay().Returns(TimeSpan.FromSeconds(10));
            delay.NextSlowDelay().Returns(TimeSpan.FromSeconds(1));
            var hasCompleted = false;

            var transition = state.Start();
            var subscription = transition.Subscribe(_ => hasCompleted = true);
            scheduler.AdvanceBy(TimeSpan.FromSeconds(10).Ticks - 1);
            subscription.Dispose();

            hasCompleted.Should().BeFalse();
        }


        public static object[] ServerExceptionsOtherThanInternalServerErrorException()
            => new[]
            {
                new object[] { new BadGatewayException() },
                new object[] { new GatewayTimeoutException() },
                new object[] { new HttpVersionNotSupportedException() },
                new object[] { new ServiceUnavailableException() }
            };
    }
}
