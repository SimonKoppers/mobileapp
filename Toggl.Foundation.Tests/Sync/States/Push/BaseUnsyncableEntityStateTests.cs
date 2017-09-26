using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using FluentAssertions;
using NSubstitute;
using Toggl.Foundation.Sync;
using Toggl.Foundation.Tests.Generators;
using Toggl.Multivac.Models;
using Toggl.PrimeRadiant;
using Toggl.Ultrawave.Exceptions;
using Xunit;

namespace Toggl.Foundation.Tests.Sync.States
{
    public abstract class BaseUnsyncableEntityStateTests
    {
        private IStartMethodTestHelper helper;

        public BaseUnsyncableEntityStateTests(IStartMethodTestHelper helper)
        {
            this.helper = helper;
        }

        [Theory]
        [ClassData(typeof(TwoParameterConstructorTestData))]
        public void ThrowsWhenArgumentsAreNull(bool hasEntity, bool hasReason)
            => helper.ThrowsWhenArgumentsAreNull(hasEntity, hasReason);

        [Fact]
        public void ThrowsWhenDatabaseOperationFails()
            => helper.ThrowsWhenDatabaseOperationFails();

        [Fact]
        public void ThrowsWhenTheReasonExceptionIsNotAnApiException()
            => helper.ThrowsWhenTheReasonExceptionIsNotAnApiException();

        [Fact]
        public void TheErrorMessageMatchesTheMessageFromTheReasonException()
            => helper.TheErrorMessageMatchesTheMessageFromTheReasonException();

        [Fact]
        public void TheSyncStatusOfTheEntityChangesToSyncFailedWhenEverythingWorks()
            => helper.TheSyncStatusOfTheEntityChangesToSyncFailedWhenEverythingWorks();

        [Fact]
        public void TheUpdatedEntityHasTheSameIdAsTheOriginalEntity()
            => helper.TheUpdatedEntityHasTheSameIdAsTheOriginalEntity();

        [Fact]
        public void TheOnlyThingThatChangesInTheUnsyncableEntityIsTheSyncStatusAndLastSyncErrorMessage()
            => helper.TheOnlyThingThatChangesInTheUnsyncableEntityIsTheSyncStatusAndLastSyncErrorMessage();

        [Fact]
        public void TheSlowRetryIsEnteredWhenTheReasonIsAnInternalServerErrorException()
            => helper.TheSlowRetryIsEnteredWhenTheReasonIsAnInternalServerErrorException();

        [Theory]
        [MemberData(nameof(ServerExceptions))]
        public void TheFastRetryIsEnteredWhenTheReasonIsAServerErrorExceptionOtherThanInternalServerErrorException(ServerErrorException reason)
            => helper.TheFastRetryIsEnteredWhenTheReasonIsAServerErrorExceptionOtherThanInternalServerErrorException(reason);

        public static object[] ServerExceptions()
            => new[]
            {
                new object[] { new BadGatewayException() },
                new object[] { new GatewayTimeoutException() },
                new object[] { new HttpVersionNotSupportedException() },
                new object[] { new ServiceUnavailableException() }
            };

        public interface IStartMethodTestHelper
        {
            void ThrowsWhenArgumentsAreNull(bool hasEntity, bool hasReason);
            void TheErrorMessageMatchesTheMessageFromTheReasonException();
            void ThrowsWhenDatabaseOperationFails();
            void TheSyncStatusOfTheEntityChangesToSyncFailedWhenEverythingWorks();
            void ThrowsWhenTheReasonExceptionIsNotAnApiException();
            void TheUpdatedEntityHasTheSameIdAsTheOriginalEntity();
            void TheOnlyThingThatChangesInTheUnsyncableEntityIsTheSyncStatusAndLastSyncErrorMessage();
            void TheSlowRetryIsEnteredWhenTheReasonIsAnInternalServerErrorException();
            void TheFastRetryIsEnteredWhenTheReasonIsAServerErrorExceptionOtherThanInternalServerErrorException(ServerErrorException reason);
        }

        internal abstract class TheStartMethod<TModel> : IStartMethodTestHelper
            where TModel : class, IBaseModel, IDatabaseSyncable
        {
            private IRepository<TModel> repository;

            public TheStartMethod()
            {
                repository = Substitute.For<IRepository<TModel>>();
            }

            public void ThrowsWhenArgumentsAreNull(bool hasEntity, bool hasReason)
            {
                TModel entity = hasEntity ? CreateDirtyEntity() : null;
                Exception reason = hasReason ? new ApiException("Test") : null;
                var state = CreateState(repository);

                Action callingStart = () => state.Start((reason, entity)).SingleAsync().Wait();

                callingStart.ShouldThrow<ArgumentNullException>();
            }

            public void ThrowsWhenDatabaseOperationFails()
            {
                var state = CreateState(repository);
                repository
                    .BatchUpdate(null, null)
                    .ReturnsForAnyArgs(_ => throw new TestException());

                Action callingStart = () => state.Start(
                    (new ApiException("test"), CreateDirtyEntity())).SingleAsync().Wait();

                callingStart.ShouldThrow<TestException>();
            }

            public void ThrowsWhenTheReasonExceptionIsNotAnApiException()
            {
                var state = CreateState(repository);
                var exception = new TestException();

                Action callingStart = () => state.Start(
                    (exception, CreateDirtyEntity())).SingleAsync().Wait();

                callingStart.ShouldThrow<TestException>().Where(e => e == exception);
            }

            public void TheErrorMessageMatchesTheMessageFromTheReasonException()
            {
                var entity = CreateDirtyEntity();
                var reason = new BadRequestException("Test.");
                var state = CreateState(repository);
                prepareBatchUpdate(entity);

                var transition = state.Start((reason, entity)).SingleAsync().Wait();
                var unsyncableEntity = ((Transition<TModel>)transition).Parameter;

                unsyncableEntity.LastSyncErrorMessage.Should().Be(reason.Message);
            }

            public void TheSyncStatusOfTheEntityChangesToSyncFailedWhenEverythingWorks()
            {
                var entity = CreateDirtyEntity();
                var state = CreateState(repository);
                prepareBatchUpdate(entity);

                var transition = state.Start((new BadRequestException(), entity)).SingleAsync().Wait();
                var unsyncableEntity = ((Transition<TModel>)transition).Parameter;

                unsyncableEntity.SyncStatus.Should().Be(SyncStatus.SyncFailed);
            }

            public void TheUpdatedEntityHasTheSameIdAsTheOriginalEntity()
            {
                var entity = CreateDirtyEntity();
                var state = CreateState(repository);
                prepareBatchUpdate(entity);

                state.Start((new BadRequestException(), entity)).SingleAsync().Wait();

                repository
                    .Received()
                    .BatchUpdate(
                        Arg.Is<IEnumerable<(long Id, TModel)>>(entities => entities.First().Id == entity.Id),
                        Arg.Any<Func<TModel, TModel, ConflictResolutionMode>>());
            }

            public void TheOnlyThingThatChangesInTheUnsyncableEntityIsTheSyncStatusAndLastSyncErrorMessage()
            {
                var entity = CreateDirtyEntity();
                var reason = new BadRequestException();
                var state = CreateState(repository);
                prepareBatchUpdate(entity);

                var transition = state.Start((reason, entity)).SingleAsync().Wait();
                var unsyncableEntity = ((Transition<TModel>)transition).Parameter;

                entity.ShouldBeEquivalentTo(unsyncableEntity, options
                    => options.IncludingProperties()
                        .Excluding(x => x.LastSyncErrorMessage)
                        .Excluding(x => x.SyncStatus));
            }

            public void TheSlowRetryIsEnteredWhenTheReasonIsAnInternalServerErrorException()
            {
                var entity = CreateDirtyEntity();
                var reason = new InternalServerErrorException();
                var state = CreateState(repository);

                var transition = state.Start((reason, entity)).Wait();
                var parameter = ((Transition<(TModel Entity, double LastWaitingTime)>)transition).Parameter;

                transition.Result.Should().Be(state.SlowRetry);
                parameter.Entity.Should().Be(entity);
                parameter.LastWaitingTime.Should().Be(0);
            }

            public void TheFastRetryIsEnteredWhenTheReasonIsAServerErrorExceptionOtherThanInternalServerErrorException(ServerErrorException reason)
            {
                var entity = CreateDirtyEntity();
                var state = CreateState(repository);

                var transition = state.Start((reason, entity)).Wait();
                var parameter = ((Transition<(TModel Entity, double LastWaitingTime)>)transition).Parameter;

                transition.Result.Should().Be(state.FastRetry);
                parameter.Entity.Should().Be(entity);
                parameter.LastWaitingTime.Should().Be(0);
            }

            private void prepareBatchUpdate(TModel entity)
            {
                repository
                    .BatchUpdate(
                        Arg.Is<IEnumerable<(long Id, TModel Entity)>>(entities => entities.First().Id == entity.Id),
                        Arg.Any<Func<TModel, TModel, ConflictResolutionMode>>())
                    .Returns(args => Observable.Return(new List<(ConflictResolutionMode, TModel)> {
                        (ConflictResolutionMode.Update, ((IEnumerable<(long, TModel Entity)>)args[0]).First().Entity) }));
            }

            protected abstract BaseUnsyncableEntityState<TModel> CreateState(IRepository<TModel> repository);

            protected abstract TModel CreateDirtyEntity();
        }
    }
}
