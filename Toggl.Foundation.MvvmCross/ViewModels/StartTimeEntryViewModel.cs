using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using MvvmCross.Core.Navigation;
using MvvmCross.Core.ViewModels;
using Toggl.Foundation.Autocomplete;
using Toggl.Foundation.Autocomplete.Suggestions;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.DTOs;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using static Toggl.Foundation.MvvmCross.Helper.Constants;

namespace Toggl.Foundation.MvvmCross.ViewModels
{
    [Preserve(AllMembers = true)]
    public sealed class StartTimeEntryViewModel : MvxViewModel<DateTimeOffset>
    {
        //Fields
        private readonly ITimeService timeService;
        private readonly ITogglDataSource dataSource;
        private readonly IMvxNavigationService navigationService;
        private readonly Subject<TextFieldInfo> infoSubject = new Subject<TextFieldInfo>();
        private readonly Subject<AutocompleteSuggestionType> queryByTypeSubject = new Subject<AutocompleteSuggestionType>();

        private long? lastProjectId;
        private IDisposable queryDisposable;
        private IDisposable elapsedTimeDisposable;

        //Properties
        public bool IsEditingDuration { get; private set; }

        public bool IsEditingStartDate { get; private set; }

        public bool IsSuggestingTags { get; private set; }

        public bool IsSuggestingProjects { get; private set; }

        public TextFieldInfo TextFieldInfo { get; set; } = TextFieldInfo.Empty;

        public TimeSpan ElapsedTime { get; private set; } = TimeSpan.Zero;

        public bool IsBillable { get; private set; } = false;

        public bool IsBillableAvailable { get; private set; } = false;

        public bool IsEditingProjects { get; private set; } = false;

        public bool IsEditingTags { get; private set; } = false;

        public DateTimeOffset StartTime { get; private set; }

        public DateTimeOffset? StopTime { get; private set; }

        public MvxObservableCollection<AutocompleteSuggestion> Suggestions { get; }
            = new MvxObservableCollection<AutocompleteSuggestion>();

        public IMvxAsyncCommand BackCommand { get; }

        public IMvxAsyncCommand DoneCommand { get; }

        public IMvxAsyncCommand ChangeDurationCommand { get; }

        public IMvxAsyncCommand ChangeStartTimeCommand { get; }

        public IMvxCommand ToggleBillableCommand { get; }

        public IMvxCommand ToggleTagSuggestionsCommand { get; }

        public IMvxCommand ToggleProjectSuggestionsCommand { get; }

        public IMvxCommand<AutocompleteSuggestion> SelectSuggestionCommand { get; }

        public StartTimeEntryViewModel(ITogglDataSource dataSource, ITimeService timeService,
            IMvxNavigationService navigationService)
        {
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));
            Ensure.Argument.IsNotNull(navigationService, nameof(navigationService));

            this.dataSource = dataSource;
            this.timeService = timeService;
            this.navigationService = navigationService;

            BackCommand = new MvxAsyncCommand(back);
            DoneCommand = new MvxAsyncCommand(done);
            ToggleBillableCommand = new MvxCommand(toggleBillable);
            ChangeDurationCommand = new MvxAsyncCommand(changeDuration);
            ChangeStartTimeCommand = new MvxAsyncCommand(changeStartTime);
            ToggleTagSuggestionsCommand = new MvxCommand(toggleTagSuggestions);
            ToggleProjectSuggestionsCommand = new MvxCommand(toggleProjectSuggestions);
            SelectSuggestionCommand = new MvxCommand<AutocompleteSuggestion>(selectSuggestion);
        }

        private void selectSuggestion(AutocompleteSuggestion suggestion)
        {
            switch (suggestion)
            {
                case QuerySymbolSuggestion querySymbolSuggestion:
                    TextFieldInfo = TextFieldInfo.WithTextAndCursor(querySymbolSuggestion.Symbol, 1);
                    break;

                case TimeEntrySuggestion timeEntrySuggestion:

                    TextFieldInfo = TextFieldInfo.WithTextAndCursor(
                        timeEntrySuggestion.Description,
                        timeEntrySuggestion.Description.Length);

                    if (timeEntrySuggestion.ProjectId.HasValue)
                    {
                        TextFieldInfo = TextFieldInfo.WithProjectInfo(
                            timeEntrySuggestion.ProjectId.Value,
                            timeEntrySuggestion.ProjectName,
                            timeEntrySuggestion.ProjectColor);
                    }
                    else
                    {
                        TextFieldInfo = TextFieldInfo.RemoveProjectInfo();
                    }

                    break;

                case ProjectSuggestion projectSuggestion:

                    TextFieldInfo = TextFieldInfo
                        .RemoveProjectQueryFromDescriptionIfNeeded()
                        .WithProjectInfo(
                            projectSuggestion.ProjectId,
                            projectSuggestion.ProjectName,
                            projectSuggestion.ProjectColor);
                    break;

                case TagSuggestion tagSuggestion:

                    TextFieldInfo = TextFieldInfo
                        .RemoveTagQueryFromDescriptionIfNeeded()
                        .AddTag(tagSuggestion);
                    break;
            }
        }

        public override void Prepare(DateTimeOffset parameter)
        {
            StartTime = parameter;

            elapsedTimeDisposable =
                timeService.CurrentDateTimeObservable.Subscribe(currentTime => ElapsedTime = currentTime - StartTime);

            var queryByTypeObservable =
                queryByTypeSubject
                    .AsObservable()
                    .SelectMany(type => dataSource.AutocompleteProvider.Query("", type));

            queryDisposable =
                infoSubject.AsObservable()
                    .StartWith(TextFieldInfo)
                    .SelectMany(dataSource.AutocompleteProvider.Query)
                    .Merge(queryByTypeObservable)
                    .Subscribe(onSuggestions);
        }

        public async override Task Initialize()
        {
            await base.Initialize();

            await ensureBillableAvailability(0);
        }

        private async void OnTextFieldInfoChanged()
        {
            infoSubject.OnNext(TextFieldInfo);

            if (TextFieldInfo.ProjectId == lastProjectId) return;
            lastProjectId = TextFieldInfo.ProjectId;
            await ensureBillableAvailability(lastProjectId ?? 0);
        }

        private void toggleTagSuggestions()
        {
            if (IsSuggestingTags)
            {
                TextFieldInfo = TextFieldInfo.RemoveTagQueryFromDescriptionIfNeeded();
                return;
            }

            var newText = TextFieldInfo.Text.Insert(TextFieldInfo.CursorPosition, QuerySymbols.TagsString);
            TextFieldInfo = TextFieldInfo.WithTextAndCursor(newText, TextFieldInfo.CursorPosition + 1);
        }

        private void toggleProjectSuggestions()
        {
            if (IsSuggestingProjects)
            {
                TextFieldInfo = TextFieldInfo.RemoveProjectQueryFromDescriptionIfNeeded();
                return;
            }

            if (TextFieldInfo.ProjectId != null)
            {
                queryByTypeSubject.OnNext(AutocompleteSuggestionType.Projects);
                return;
            }

            var newText = TextFieldInfo.Text.Insert(TextFieldInfo.CursorPosition, QuerySymbols.ProjectsString);
            TextFieldInfo = TextFieldInfo.WithTextAndCursor(newText, TextFieldInfo.CursorPosition + 1);
        }

        private void toggleBillable() => IsBillable = !IsBillable;

        private async Task changeStartTime()
        {
            IsEditingStartDate = true;

            var currentTime = timeService.CurrentDateTime;
            var minDate = currentTime.AddHours(-MaxTimeEntryDurationInHours);

            var parameters = DatePickerParameters.WithDates(StartTime, minDate, currentTime);
            StartTime = await navigationService
                .Navigate<SelectDateTimeViewModel, DatePickerParameters, DateTimeOffset>(parameters)
                .ConfigureAwait(false);

            IsEditingStartDate = false;
        }

        private Task back() => navigationService.Close(this);

        private async Task changeDuration()
        {
            IsEditingDuration = true;

            var currentDuration = DurationParameter.WithStartAndStop(StartTime, null);
            var selectedDuration = await navigationService
                .Navigate<EditDurationViewModel, DurationParameter, DurationParameter>(currentDuration)
                .ConfigureAwait(false);

            StartTime = selectedDuration.Start;

            IsEditingDuration = false;
        }

        private async Task done()
        {
            await dataSource.User.Current()
                .Select(user => new StartTimeEntryDTO
                {
                    UserId = user.Id,
                    StartTime = StartTime,
                    Billable = IsBillable,
                    Description = TextFieldInfo.Text,
                    ProjectId = TextFieldInfo.ProjectId,
                    WorkspaceId = user.DefaultWorkspaceId
                })
                .SelectMany(dataSource.TimeEntries.Start);

            await navigationService.Close(this);
        }

        private void onSuggestions(IEnumerable<AutocompleteSuggestion> suggestions)
        {
            var firstSuggestion = suggestions.FirstOrDefault();
            IsSuggestingTags = firstSuggestion is TagSuggestion;
            IsSuggestingProjects = firstSuggestion is ProjectSuggestion;

            Suggestions.Clear();
            Suggestions.AddRange(suggestions.Distinct(AutocompleteSuggestionComparer.Instance));
        }

        private async Task ensureBillableAvailability(long projectId)
        {
            var workspaceObservable = projectId == 0 
                ? dataSource.Workspaces.GetDefault().Select(ws => (Workspace: ws, DefaultToBillable: false))
                : dataSource.Projects.GetById(projectId)
                    .SelectMany(project => 
                        dataSource.Workspaces
                            .GetById(project.WorkspaceId)
                            .Select(ws => (Workspace: ws, DefaultToBillable: project.Billable ?? false)));
           
            (IsBillableAvailable, IsBillable) =
                await workspaceObservable
                    .SelectMany(tuple =>
                        dataSource.Workspaces
                            .WorkspaceHasFeature(tuple.Workspace.Id, WorkspaceFeatureId.Pro)
                            .Select(isAvailable => (IsBillableAvailable: isAvailable, IsBillable: isAvailable && tuple.DefaultToBillable)));
        }
    }
}
