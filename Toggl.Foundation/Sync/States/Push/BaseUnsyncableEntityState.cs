using System;
using System.Reactive.Linq;
using Toggl.Foundation.Sync;
using Toggl.Multivac.Models;
using Toggl.PrimeRadiant;
using Toggl.Ultrawave.Exceptions;

namespace Toggl.Foundation.Tests.Sync.States
{
    internal abstract class BaseUnsyncableEntityState<TModel>
        where TModel : IBaseModel, IDatabaseSyncable
    {
        private IRepository<TModel> repository;
        private readonly IRetryDelayService delay;

        public StateResult<TModel> MarkedAsUnsyncable { get; } = new StateResult<TModel>();
        public StateResult CheckServerStatus { get; } = new StateResult();

        public BaseUnsyncableEntityState(IRepository<TModel> repository, IRetryDelayService delay)
        {
            this.repository = repository;
            this.delay = delay;
        }

        public IObservable<ITransition> Start((Exception Reason, TModel Entity) failedPush)
            => failedPush.Reason == null || failedPush.Entity == null
                ? failBecauseOfNullArguments(failedPush)
                : failedPush.Reason is ApiException
                    ? failedPush.Reason is ServerErrorException
                        ? enterRetryLoop(failedPush)
                        : markAsUnsyncable(failedPush.Entity, failedPush.Reason.Message)
                    : failBecauseOfUnexpectedError(failedPush.Reason);

        private IObservable<ITransition> failBecauseOfNullArguments((Exception Reason, TModel Entity) failedPush)
            => Observable.Throw<Transition<TModel>>(new ArgumentNullException(
                failedPush.Reason == null
                    ? nameof(failedPush.Reason)
                    : nameof(failedPush.Entity)));

        private IObservable<ITransition> failBecauseOfUnexpectedError(Exception reason)
            => Observable.Throw<Transition<TModel>>(reason);

        private IObservable<ITransition> markAsUnsyncable(TModel entity, string reason)
            => repository
                .UpdateWithConflictResolution(entity.Id, CreateUnsyncableFrom(entity, reason), overwriteIfLocalEntityDidNotChange(entity))
                .Do(_ => delay.Reset())
                .Select(updated => MarkedAsUnsyncable.Transition(CopyFrom(updated.Entity)));

        private IObservable<ITransition> enterRetryLoop((Exception Reason, TModel Entity) failedPush)
            => Observable.Return(CheckServerStatus.Transition());

        private Func<TModel, TModel, ConflictResolutionMode> overwriteIfLocalEntityDidNotChange(TModel local)
            => (currentLocal, _) => HasChanged(local, currentLocal)
                ? ConflictResolutionMode.Ignore
                : ConflictResolutionMode.Update;

        protected abstract bool HasChanged(TModel original, TModel current);

        protected abstract TModel CreateUnsyncableFrom(TModel entity, string reson);

        protected abstract TModel CopyFrom(TModel entity);
    }
}
