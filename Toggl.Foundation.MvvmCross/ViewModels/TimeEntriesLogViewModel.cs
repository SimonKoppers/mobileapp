using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using MvvmCross.Core.Navigation;
using MvvmCross.Core.ViewModels;
using PropertyChanged;
using Toggl.Foundation.DTOs;
using Toggl.Foundation.DataSources;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.PrimeRadiant.Models;
using System.Reactive.Disposables;
using Toggl.Foundation.MvvmCross.Collections;

namespace Toggl.Foundation.MvvmCross.ViewModels
{
    [Preserve(AllMembers = true)]
    public sealed class TimeEntriesLogViewModel : MvxViewModel
    {
        private readonly CompositeDisposable disposeBag = new CompositeDisposable();

        private readonly ITimeService timeService;
        private readonly ITogglDataSource dataSource;
        private readonly IMvxNavigationService navigationService;

        public bool IsWelcome { get; set; }

        public MvxObservableCollection<TimeEntryViewModelCollection> TimeEntries { get; }
            = new MvxObservableCollection<TimeEntryViewModelCollection>();

        [DependsOn(nameof(TimeEntries))]
        public bool IsEmpty => !TimeEntries.Any();

        [DependsOn(nameof(IsWelcome))]
        public string EmptyStateTitle
            => IsWelcome
            ? Resources.TimeEntriesLogEmptyStateWelcomeTitle
            : Resources.TimeEntriesLogEmptyStateTitle;

        [DependsOn(nameof(IsWelcome))]
        public string EmptyStateText
            => IsWelcome
            ? Resources.TimeEntriesLogEmptyStateWelcomeText
            : Resources.TimeEntriesLogEmptyStateText;

        public IMvxAsyncCommand<TimeEntryViewModel> EditCommand { get; }

        public IMvxAsyncCommand<TimeEntryViewModel> ContinueTimeEntryCommand { get; }

        public TimeEntriesLogViewModel(ITogglDataSource dataSource, ITimeService timeService,
            IMvxNavigationService navigationService)
        {
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));
            Ensure.Argument.IsNotNull(navigationService, nameof(navigationService));

            this.dataSource = dataSource;
            this.timeService = timeService;
            this.navigationService = navigationService;

            EditCommand = new MvxAsyncCommand<TimeEntryViewModel>(edit);
            ContinueTimeEntryCommand = new MvxAsyncCommand<TimeEntryViewModel>(continueTimeEntry);
        }

        public async override Task Initialize()
        {
            await base.Initialize();

            var timeEntries = await dataSource.TimeEntries.GetAll();

            timeEntries
                .Where(isNotRunning)
                .OrderByDescending(te => te.Start)
                .Select(te => new TimeEntryViewModel(te))
                .GroupBy(te => te.Start.Date)
                .Select(grouping => new TimeEntryViewModelCollection(grouping.Key, grouping))
                .ForEach(addTimeEntries);

            var deleteDisposable = 
                dataSource.TimeEntries.TimeEntryDeleted
                          .Subscribe(onTimeEntryUpdated);

            var updateObservable =
                dataSource.TimeEntries.TimeEntryUpdated
                          .Do(onTimeEntryUpdated)
                          .Where(x => !x.IsDeleted);

            var updateDisposable =
                dataSource.TimeEntries.TimeEntryCreated
                    .Where(isNotRunning)
                    .Merge(updateObservable)
                    .Subscribe(onTimeEntryCreated);

            disposeBag.Add(deleteDisposable);
            disposeBag.Add(updateDisposable);
        }

        private void addTimeEntries(TimeEntryViewModelCollection collection)
        {
            TimeEntries.Add(collection);
            
            RaisePropertyChanged(nameof(IsEmpty));
        }

        private void onTimeEntryUpdated(IDatabaseTimeEntry timeEntry)
        {
            var collection = TimeEntries.SingleOrDefault(c => c.Date == timeEntry.Start.Date);
            if (collection == null) return;

            var viewModel = collection.SingleOrDefault(vm => vm.Id == timeEntry.Id);
            if (viewModel == null) return;

            collection.Remove(viewModel);
            RaisePropertyChanged(nameof(IsEmpty));
        }

        private void onTimeEntryCreated(IDatabaseTimeEntry timeEntry)
        {
            var indexDate = timeEntry.Start.Date;
            var timeEntriesInDay = new List<TimeEntryViewModel> { new TimeEntryViewModel(timeEntry) };

            var collection = TimeEntries.FirstOrDefault(x => x.Date == indexDate);
            if (collection != null)
            {
                timeEntriesInDay.AddRange(collection);
                TimeEntries.Remove(collection);
            }

            var newCollection = new TimeEntryViewModelCollection(indexDate, timeEntriesInDay.OrderByDescending(te => te.Start));

            var foundIndex = TimeEntries.IndexOf(TimeEntries.FirstOrDefault(x => x.Date < indexDate));
            var indexToInsert = foundIndex == -1 ? TimeEntries.Count : foundIndex;
            TimeEntries.Insert(indexToInsert, newCollection);

            RaisePropertyChanged(nameof(IsEmpty));
        }

        private bool isNotRunning(IDatabaseTimeEntry timeEntry) => timeEntry.Stop != null;

        private Task edit(TimeEntryViewModel timeEntryViewModel)
            => navigationService.Navigate<EditTimeEntryViewModel, long>(timeEntryViewModel.Id);

        private async Task continueTimeEntry(TimeEntryViewModel timeEntryViewModel)
        {
            await dataSource.TimeEntries.Stop(timeService.CurrentDateTime)
                            .OnErrorResumeNext(Observable.Return(default(IDatabaseTimeEntry)));

            await dataSource.User
                .Current()
                .Select(user => new StartTimeEntryDTO
                {
                    UserId = user.Id,
                    WorkspaceId = user.DefaultWorkspaceId,
                    Billable = timeEntryViewModel.Billable,
                    StartTime = timeService.CurrentDateTime, 
                    ProjectId = timeEntryViewModel.ProjectId,
                    Description = timeEntryViewModel.Description
                })
                .SelectMany(dataSource.TimeEntries.Start);
        }
    }
}
