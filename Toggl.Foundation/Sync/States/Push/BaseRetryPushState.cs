using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Toggl.Ultrawave;

namespace Toggl.Foundation.Sync.States.Push
{
    public abstract class BaseRetryPushState<TModel>
    {
        public StateResult<(TModel Entity, double LastWaitingTime)> ServerIsUnavailable { get; } = new StateResult<(TModel, double)>();
        public StateResult<TModel> ServerIsAvailable { get; } = new StateResult<TModel>();

        private ITogglApi api;
        private IScheduler scheduler;
        private Random rnd;

        public BaseRetryPushState(ITogglApi api, IScheduler scheduler, Random rnd)
        {
            this.api = api;
            this.scheduler = scheduler;
            this.rnd = rnd;
        }

        public IObservable<ITransition> Start((TModel Entity, double LastWaitingTime) failedPush)
            => api.Status.Get()
                .SelectMany(isAvailable => isAvailable
                    ? proceed(failedPush.Entity)
                    : retry(failedPush));

        private IObservable<ITransition> retry((TModel Entity, double LastWaitingTime) failedPush)
        {
            var timeToWait = Math.Min(TimeSpan.MaxValue.TotalSeconds, nextWaitingTime(failedPush.LastWaitingTime));
            return Observable.Return(ServerIsUnavailable.Transition((failedPush.Entity, timeToWait)))
                .Delay(TimeSpan.FromSeconds(timeToWait), scheduler);
        }

        private IObservable<ITransition> proceed(TModel entity)
            => Observable.Return(ServerIsAvailable.Transition(entity));

        private double nextWaitingTime(double lastWaitingTime)
        {
            if (lastWaitingTime <= 0)
                return FirstWaitingTime;

            var factor = rnd.NextDouble() * (MaximumFactor - MinimumFactor) + MinimumFactor;
            return lastWaitingTime * factor;
        }

        protected abstract double FirstWaitingTime { get; }

        protected abstract double MinimumFactor { get; }

        protected abstract double MaximumFactor { get; }
    }
}
