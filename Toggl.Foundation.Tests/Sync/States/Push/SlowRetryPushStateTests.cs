using System;
using System.Reactive.Concurrency;
using Toggl.Foundation.Sync.States.Push;
using Toggl.Ultrawave;

namespace Toggl.Foundation.Tests.Sync.States.Push
{
    public sealed class SlowRetryPushStateTests : BaseRetryPushStateTests
    {
        protected override BaseRetryPushState<TestModel> CreateState(ITogglApi api, IScheduler scheduler, Random rnd)
            => new SlowRetryPushState<TestModel>(api, scheduler, rnd);

        protected override double DefaultWaitingTime => 60;
        protected override double MinProlongingFactor => 1.5;
        protected override double MaxProlongingFactor => 2;
    }
}
