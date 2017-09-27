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
            => api.Status.IsAvailable()
                .SelectMany(proceed)
                .Catch((Exception e) => delayedRetry(getDelay(e)));

        private IObservable<ITransition> proceed
            => Observable.Return(ServerIsAvailable.Transition());

        private IObservable<ITransition> delayedRetry(TimeSpan period)
            => Observable.Return(Retry.Transition()).Delay(period, scheduler);

        private TimeSpan getDelay(Exception exception)
            => exception is InternalServerErrorException
                ? delay.NextSlowDelay()
                : delay.NextFastDelay();
    }
}
