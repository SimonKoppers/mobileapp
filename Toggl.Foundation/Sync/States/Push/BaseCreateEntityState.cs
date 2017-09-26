using System;
using System.Reactive.Linq;
using Toggl.Multivac.Models;
using Toggl.PrimeRadiant;
using Toggl.Ultrawave;

namespace Toggl.Foundation.Sync.States
{
    internal abstract class BaseCreateEntityState<TModel>
        where TModel : class, IBaseModel, IDatabaseSyncable
    {
        private readonly ITogglApi api;
        private readonly IRepository<TModel> repository;
        private readonly IRetryDelayService delay;

        public StateResult<(Exception, TModel)> CreatingFailed { get; } = new StateResult<(Exception, TModel)>();
        public StateResult<TModel> CreatingFinished { get; } = new StateResult<TModel>();

        public BaseCreateEntityState(ITogglApi api, IRepository<TModel> repository, IRetryDelayService delay)
        {
            this.api = api;
            this.repository = repository;
            this.delay = delay;
        }

        public IObservable<ITransition> Start(TModel entity)
            => create(entity)
                .SelectMany(overwrite(entity))
                .Do(_ => delay.Reset())
                .Select(CreatingFinished.Transition)
                .Catch(fail(entity));

        private IObservable<TModel> create(TModel entity)
            => entity == null
                ? Observable.Throw<TModel>(new ArgumentNullException(nameof(entity)))
                : Create(api, entity);

        private Func<TModel, IObservable<TModel>> overwrite(TModel entity)
            => createdEntity => repository.Update(entity.Id, createdEntity).Select(CopyFrom);

        private Func<Exception, IObservable<ITransition>> fail(TModel entity)
            => e => Observable.Return(CreatingFailed.Transition((e, entity)));

        protected abstract IObservable<TModel> Create(ITogglApi api, TModel entity);

        protected abstract TModel CopyFrom(TModel entity);
    }
}
