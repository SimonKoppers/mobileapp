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

        public StateResult<TModel> MarkedAsUnsyncable { get; } = new StateResult<TModel>();
        public StateResult<(TModel Entity, double LastWaitingTime)> FastRetry { get; } = new StateResult<(TModel, double)>();
        public StateResult<(TModel Entity, double LastWaitingTime)> SlowRetry { get; } = new StateResult<(TModel, double)>();

        public BaseUnsyncableEntityState(IRepository<TModel> repository)
        {
            this.repository = repository;
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
                .Select(updated => MarkedAsUnsyncable.Transition(CopyFrom(updated.Entity)));

        private IObservable<ITransition> enterRetryLoop((Exception Reason, TModel Entity) failedPush)
            => failedPush.Reason is InternalServerErrorException
                ? Observable.Return(SlowRetry.Transition((failedPush.Entity, 0)))
                : Observable.Return(FastRetry.Transition((failedPush.Entity, 0)));

        private Func<TModel, TModel, ConflictResolutionMode> overwriteIfLocalEntityDidNotChange(TModel local)
            => (currentLocal, _) => HasChanged(local, currentLocal)
                ? ConflictResolutionMode.Ignore
                : ConflictResolutionMode.Update;

        protected abstract bool HasChanged(TModel original, TModel current);

        protected abstract TModel CreateUnsyncableFrom(TModel entity, string reson);

        protected abstract TModel CopyFrom(TModel entity);
    }
}
