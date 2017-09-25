using System;
using System.Reactive.Concurrency;
using Toggl.Ultrawave;

namespace Toggl.Foundation.Sync.States.Push
{
    public sealed class FastRetryPushState<TModel> : BaseRetryPushState<TModel>
    {
        public FastRetryPushState(ITogglApi api, IScheduler scheduler, Random rnd)
            : base(api, scheduler, rnd)
        {
        }

        protected override double FirstWaitingTime => 10;
        protected override double MinimumFactor => 1;
        protected override double MaximumFactor => 1.5;
    }
}
