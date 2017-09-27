using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using MvvmCross.Core.Navigation;
using MvvmCross.Core.ViewModels;
using Toggl.Foundation.Autocomplete.Suggestions;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.MvvmCross.Collections;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;

namespace Toggl.Foundation.MvvmCross.ViewModels
{
    [Preserve(AllMembers = true)]
    public sealed class SelectProjectViewModel : MvxViewModel<long?, long?>
    {
        private readonly ITogglDataSource dataSource;
        private readonly IMvxNavigationService navigationService;
        private readonly Subject<string> infoSubject = new Subject<string>();
        private long? projectId;

        public string Text { get; set; } = "";

        public IMvxAsyncCommand CloseCommand { get; }

        public IMvxAsyncCommand<ProjectSuggestion> SelectProjectCommand { get; }

        public MvxObservableCollection<WorkspaceGroupedCollection<ProjectSuggestion>> Suggestions { get; }
            = new MvxObservableCollection<WorkspaceGroupedCollection<ProjectSuggestion>>();

        public SelectProjectViewModel(ITogglDataSource dataSource, IMvxNavigationService navigationService)
        {
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            Ensure.Argument.IsNotNull(navigationService, nameof(navigationService));

            this.dataSource = dataSource;
            this.navigationService = navigationService;

            CloseCommand = new MvxAsyncCommand(close);
            SelectProjectCommand = new MvxAsyncCommand<ProjectSuggestion>(selectProject);
        }

        public override void Prepare(long? parameter)
            => projectId = parameter;

        public override async Task Initialize()
        {
            await base.Initialize();

            infoSubject.AsObservable()
                       .StartWith(Text)
                       .SelectMany(text => dataSource.AutocompleteProvider.Query(text, AutocompleteSuggestionType.Projects))
                       .Select(suggestions => suggestions.Cast<ProjectSuggestion>())
                       .Subscribe(onSuggestions);
        }

        private void OnTextChanged()
        {
            infoSubject.OnNext(Text);
        }

        private void onSuggestions(IEnumerable<ProjectSuggestion> suggestions)
        {
            Suggestions.Clear();

            suggestions
                .Where(suggestion => !string.IsNullOrEmpty(suggestion.Workspace))
                .GroupBy(suggestion => suggestion.Workspace)
                .Select(collectionWithNoProject)
                .ForEach(Suggestions.Add);
        }

        private WorkspaceGroupedCollection<ProjectSuggestion> collectionWithNoProject(
            IGrouping<string, ProjectSuggestion> grouping)
        {
            var collection = new WorkspaceGroupedCollection<ProjectSuggestion>(grouping.Key, grouping);
            collection.Insert(0, ProjectSuggestion.NoProject);
            return collection;
        }

        private Task close()
            => navigationService.Close(this, projectId);

        private Task selectProject(ProjectSuggestion project)
        => navigationService.Close(this, project.ProjectId == 0 ? null : (long?)project.ProjectId);
    }
}
