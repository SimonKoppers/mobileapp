using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using System.Linq;
using System.Reactive.Subjects;
using Toggl.Foundation.Models;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Models;
using Toggl.Foundation.DTOs;

namespace Toggl.Foundation.DataSources
{
    internal sealed class TimeEntriesDataSource : ITimeEntriesSource
    {
        private long? currentlyRunningTimeEntryId;

        private readonly IIdProvider idProvider;
        private readonly ITimeService timeService;
        private readonly IRepository<IDatabaseTimeEntry> repository;
        private readonly Subject<IDatabaseTimeEntry> timeEntryCreatedSubject = new Subject<IDatabaseTimeEntry>();
        private readonly Subject<IDatabaseTimeEntry> timeEntryUpdatedSubject = new Subject<IDatabaseTimeEntry>();
        private readonly Subject<IDatabaseTimeEntry> timeEntryDeletedSubject = new Subject<IDatabaseTimeEntry>();

        public IObservable<bool> IsEmpty { get; }

        public IObservable<IDatabaseTimeEntry> CurrentlyRunningTimeEntry { get; }

        public IObservable<IDatabaseTimeEntry> TimeEntryCreated { get; }

        public IObservable<IDatabaseTimeEntry> TimeEntryUpdated { get; }

        public IObservable<IDatabaseTimeEntry> TimeEntryDeleted { get; }

        public TimeEntriesDataSource(IIdProvider idProvider, IRepository<IDatabaseTimeEntry> repository, ITimeService timeService)
        {
            Ensure.Argument.IsNotNull(idProvider, nameof(idProvider));
            Ensure.Argument.IsNotNull(repository, nameof(repository));
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));

            this.repository = repository;
            this.idProvider = idProvider;
            this.timeService = timeService;

            TimeEntryCreated = timeEntryCreatedSubject.AsObservable();
            TimeEntryUpdated = timeEntryUpdatedSubject.AsObservable();
            TimeEntryDeleted = timeEntryDeletedSubject.AsObservable();
            CurrentlyRunningTimeEntry =
                repository.GetAll(te => te.Stop == null)
                    .Select(tes => tes.SingleOrDefault())
                    .StartWith()
                    .Merge(TimeEntryCreated.Where(te => te.Stop == null))
                    .Merge(TimeEntryUpdated.Where(te => te.Id == currentlyRunningTimeEntryId))
                    .Select(runningTimeEntry)
                    .Do(setRunningTimeEntryId);

            IsEmpty =
                Observable.Return(default(IDatabaseTimeEntry))
                    .StartWith()
                    .Merge(TimeEntryUpdated)
                    .Merge(TimeEntryCreated)
                    .SelectMany(_ => GetAll())
                    .Select(timeEntries => !timeEntries.Any());
        }

        public IObservable<IEnumerable<IDatabaseTimeEntry>> GetAll()
            => repository.GetAll(te => !te.IsDeleted);

        public IObservable<IEnumerable<IDatabaseTimeEntry>> GetAll(Func<IDatabaseTimeEntry, bool> predicate)
            => repository.GetAll(predicate).Select(tes => tes.Where(te => !te.IsDeleted));

        public IObservable<IDatabaseTimeEntry> GetById(long id)
            => repository.GetById(id);

        public IObservable<Unit> Delete(long id)
            => repository.GetById(id)
                         .Select(TimeEntry.DirtyDeleted)
                         .SelectMany(repository.Update)
                         .Select(TimeEntry.DirtyDeleted)
                         .Do(timeEntryUpdatedSubject.OnNext)
                         .IgnoreElements()
                         .Cast<Unit>();

        public IObservable<IDatabaseTimeEntry> Start(StartTimeEntryDTO dto)
            => idProvider.GetNextIdentifier()
                .Apply(TimeEntry.Builder.Create)
                .SetUserId(dto.UserId)
                .SetStart(dto.StartTime)
                .SetBillable(dto.Billable)
                .SetProjectId(dto.ProjectId)
                .SetDescription(dto.Description)
                .SetWorkspaceId(dto.WorkspaceId)
                .SetAt(timeService.CurrentDateTime)
                .SetSyncStatus(SyncStatus.SyncNeeded)
                .Build()
                .Apply(Create);

        public IObservable<IDatabaseTimeEntry> Stop(DateTimeOffset stopTime)
            => repository
                    .GetAll(te => te.Stop == null)
                    .SelectMany(timeEntries =>
                        timeEntries.Single()
                            .With(stopTime)
                            .Apply(this.Update));

        public IObservable<IDatabaseTimeEntry> Update(EditTimeEntryDto dto)
            => repository.GetById(dto.Id)
                         .Select(te => createUpdatedTimeEntry(te, dto))
                         .SelectMany(this.Update);

        public IObservable<IDatabaseTimeEntry> Create(IDatabaseTimeEntry entity)
            => repository
                .Create(entity)
                .Do(timeEntryCreatedSubject.OnNext);

        public IObservable<IDatabaseTimeEntry> Update(long id, IDatabaseTimeEntry entity)
            => repository.Update(id, entity)
                .Do(timeEntryUpdatedSubject.OnNext);

        public IObservable<IEnumerable<(ConflictResolutionMode ResolutionMode, IDatabaseTimeEntry Entity)>> BatchUpdate(
            IEnumerable<(long Id, IDatabaseTimeEntry Entity)> entities,
            Func<IDatabaseTimeEntry, IDatabaseTimeEntry, ConflictResolutionMode> conflictResolution)
            => repository
                .BatchUpdate(entities, conflictResolution)
                .Do(updatedEntities =>
                    updatedEntities
                    .Where(tuple => tuple.ResolutionMode != ConflictResolutionMode.Ignore)
                    .ForEach(handleBatchUpdateTuple));

        private void handleBatchUpdateTuple((ConflictResolutionMode ResolutionMode, IDatabaseTimeEntry Entity) tuple)
        {
            switch (tuple.ResolutionMode)
            {
                case ConflictResolutionMode.Delete:
                    timeEntryDeletedSubject.OnNext(tuple.Entity);
                    return;

                case ConflictResolutionMode.Create:
                    timeEntryCreatedSubject.OnNext(tuple.Entity);
                    return;

                case ConflictResolutionMode.Update:
                    timeEntryUpdatedSubject.OnNext(tuple.Entity);
                    return;
            }
        }

        private TimeEntry createUpdatedTimeEntry(IDatabaseTimeEntry timeEntry, EditTimeEntryDto dto)
            => TimeEntry.Builder.Create(dto.Id)
                        .SetDescription(dto.Description)
                        .SetStop(dto.StopTime)
                        .SetStart(dto.StartTime)
                        .SetBillable(dto.Billable)
                        .SetProjectId(dto.ProjectId)
                        .SetTagIds(timeEntry.TagIds)
                        .SetTaskId(timeEntry.TaskId)
                        .SetUserId(timeEntry.UserId)
                        .SetIsDeleted(timeEntry.IsDeleted)
                        .SetCreatedWith(timeEntry.CreatedWith)
                        .SetWorkspaceId(timeEntry.WorkspaceId)
                        .SetServerDeletedAt(timeEntry.ServerDeletedAt)
                        .SetAt(timeService.CurrentDateTime)
                        .SetSyncStatus(SyncStatus.SyncNeeded)
                        .Build();

        private IDatabaseTimeEntry runningTimeEntry(IDatabaseTimeEntry timeEntry)
        {
            if (timeEntry == null || timeEntry.Stop != null)
                return null;
           
            return TimeEntry.From(timeEntry);
        }

        private void setRunningTimeEntryId(IDatabaseTimeEntry timeEntry)
        {
            currentlyRunningTimeEntryId = timeEntry?.Id;
        }
    }
}
