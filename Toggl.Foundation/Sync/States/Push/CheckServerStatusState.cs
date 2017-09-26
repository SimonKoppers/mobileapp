using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Toggl.Ultrawave;
using Toggl.Ultrawave.Exceptions;

namespace Toggl.Foundation.Sync.States.Push
{
    internal sealed class CheckServerStatusState
    {
        public StateResult Retry { get; } = new StateResult();
        public StateResult ServerIsAvailable { get; } = new StateResult();

        private ITogglApi api;
        private IScheduler scheduler;
        private IRetryDelayService delay;

        public CheckServerStatusState(ITogglApi api, IScheduler scheduler, IRetryDelayService delay)
        {
            this.api = api;
            this.scheduler = scheduler;
            this.delay = delay;
        }

        public IObservable<ITransition> Start()
            => api.Status.Get()
                .SelectMany(proceed)
                .Catch((InternalServerErrorException e) => delayedRetry(delay.NextSlowDelay()))
                .Catch((ServerErrorException e) => delayedRetry(delay.NextFastDelay()));

        private IObservable<ITransition> proceed(bool isAvaialbe)
            => isAvaialbe
                ? Observable.Return(ServerIsAvailable.Transition())
                : delayedRetry(delay.NextFastDelay());

        private IObservable<ITransition> retry(Exception exception)
            => delayedRetry(getDelay(exception));

        private IObservable<ITransition> delayedRetry(TimeSpan period)
            => Observable.Return((ITransition)Retry.Transition())
                .Delay(period, scheduler);

        private TimeSpan getDelay(Exception exception)
            => exception is InternalServerErrorException
                ? delay.NextSlowDelay()
                : delay.NextFastDelay();
    }
}
