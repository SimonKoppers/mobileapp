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

        public BaseRetryPushState(ITogglApi api, IScheduler scheduler)
        {
            this.api = api;
            this.scheduler = scheduler;
        }

        public IObservable<ITransition> Start((TModel Entity, double LastWaitingTime) failedPush)
            => api.Status.Get()
                .SelectMany(isAvailable => isAvailable
                    ? proceed(failedPush.Entity)
                    : retry(failedPush));

        private IObservable<ITransition> retry((TModel Entity, double LastWaitingTime) failedPush)
        {
            var timeToWait = Math.Min(TimeSpan.MaxValue.TotalSeconds, NextWaitingTime(failedPush.LastWaitingTime));
            return Observable.Return(ServerIsUnavailable.Transition((failedPush.Entity, timeToWait)))
                .Delay(TimeSpan.FromSeconds(timeToWait), scheduler);
        }

        private IObservable<ITransition> proceed(TModel entity)
            => Observable.Return(ServerIsAvailable.Transition(entity));

        protected abstract double NextWaitingTime(double lastWaitingTime);
    }
}
