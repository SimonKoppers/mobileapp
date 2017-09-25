using System;
using System.Reactive.Concurrency;
using Toggl.Ultrawave;

namespace Toggl.Foundation.Sync.States.Push
{
    public sealed class FastRetryPushState<TModel> : BaseRetryPushState<TModel>
    {
        private const double initialWaitingTime = 10;
        private readonly Random rnd;

        public FastRetryPushState(ITogglApi api, IScheduler scheduler, Random rnd)
            : base(api, scheduler)
        {
            this.rnd = rnd;
        }

        protected override double NextWaitingTime(double lastWaitingTime)
        {
            if (lastWaitingTime <= 0)
                return initialWaitingTime;

            var factor = rnd.NextDouble() / 2 + 1;
            return lastWaitingTime * factor;
        }
    }
}
