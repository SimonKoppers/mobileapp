using System;
using System.Reactive.Concurrency;
using Toggl.Foundation.Sync.States.Push;
using Toggl.Ultrawave;

namespace Toggl.Foundation.Tests.Sync.States.Push
{
    public sealed class FastRetryPushStateTests : BaseRetryPushStateTests
    {
        protected override BaseRetryPushState<TestModel> CreateState(ITogglApi api, IScheduler scheduler, Random rnd)
            => new FastRetryPushState<TestModel>(api, scheduler, rnd);

        protected override double DefaultWaitingTime => 10;
        protected override double MinProlongingFactor => 1;
        protected override double MaxProlongingFactor => 1.5;
    }
}
