using System;
using System.Reactive.Concurrency;
using Toggl.Ultrawave;

namespace Toggl.Foundation.Sync.States.Push
{
    public sealed class SlowRetryPushState<TModel> : BaseRetryPushState<TModel>
    {
        public SlowRetryPushState(ITogglApi api, IScheduler scheduler, Random rnd)
            : base(api, scheduler, rnd)
        {
        }

        protected override double FirstWaitingTime => 60;
        protected override double MinimumFactor => 1.5;
        protected override double MaximumFactor => 2;
    }
}
