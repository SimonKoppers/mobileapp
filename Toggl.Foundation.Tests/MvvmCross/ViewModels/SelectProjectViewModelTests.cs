using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Toggl.Foundation.Autocomplete;
using Toggl.Foundation.Autocomplete.Suggestions;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.Tests.Generators;
using Toggl.PrimeRadiant.Models;
using Xunit;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels
{
    public sealed class SelectProjectViewModelTests
    {
        public abstract class SelectProjectViewModelTest : BaseViewModelTests<SelectProjectViewModel>
        {
            protected override SelectProjectViewModel CreateViewModel()
                => new SelectProjectViewModel(DataSource, NavigationService);
        }

        public sealed class TheConstructor : SelectProjectViewModelTest
        {
            [Theory]
            [ClassData(typeof(TwoParameterConstructorTestData))]
            public void ThrowsIfAnyOfTheArgumentsIsNull(bool useDataSource, bool useNavigationService)
            {
                var dataSource = useDataSource ? DataSource : null;
                var navigationService = useNavigationService ? NavigationService : null;

                Action tryingToConstructWithEmptyParameters =
                    () => new SelectProjectViewModel(dataSource, navigationService);

                tryingToConstructWithEmptyParameters
                    .ShouldThrow<ArgumentNullException>();
            }
        }

        public sealed class TheCloseCommand : SelectProjectViewModelTest
        {
            [Fact]
            public async Task ClosesTheViewModel()
            {
                await ViewModel.CloseCommand.ExecuteAsync();

                await NavigationService.Received().Close(Arg.Is(ViewModel), Arg.Any<long?>());
            }

            [Fact]
            public async Task ReturnsTheSameIdThatWasPassedToTheViewModel()
            {
                long id = 13;
                ViewModel.Prepare(id);

                await ViewModel.CloseCommand.ExecuteAsync();

                await NavigationService.Received()
                    .Close(Arg.Is(ViewModel), Arg.Is(id));
            }
        }

        public sealed class TheSelectProjectCommand : SelectProjectViewModelTest
        {
            [Fact]
            public async Task ClosesTheViewModel()
            {
                await ViewModel.SelectProjectCommand.ExecuteAsync(ProjectSuggestion.NoProject);

                await NavigationService.Received().Close(Arg.Is(ViewModel), Arg.Any<long?>());
            }

            [Fact]
            public async Task ReturnsTheSelectedProjectId()
            {
                var project = Substitute.For<IDatabaseProject>();
                project.Id.Returns(13);
                var selectedProject = new ProjectSuggestion(project);
                await ViewModel.SelectProjectCommand.ExecuteAsync(selectedProject);

                await NavigationService.Received()
                    .Close(Arg.Is(ViewModel), Arg.Is(selectedProject.ProjectId));
            }

            [Fact] 
            public async Task ReturnsNullIfNoProjectWasSelected()
            {
                await ViewModel.SelectProjectCommand.ExecuteAsync(ProjectSuggestion.NoProject);

                await NavigationService.Received()
                    .Close(Arg.Is(ViewModel), Arg.Is<long?>(id => id == null));
            }
        }

        public sealed class TheTextProperty : SelectProjectViewModelTest
        {
            [Fact]
            public async Task WhenChangedQueriesTheAutocompleteProvider()
            {
                var text = "Some text";
                var autocompleteProvider = Substitute.For<IAutocompleteProvider>();
                DataSource.AutocompleteProvider.Returns(autocompleteProvider);
                await ViewModel.Initialize();

                ViewModel.Text = text;

                await autocompleteProvider.Received().Query(Arg.Is(text), Arg.Is(AutocompleteSuggestionType.Projects));
            }
        }

        public sealed class TheSuggestionsProperty : SelectProjectViewModelTest
        {
            private IEnumerable<ProjectSuggestion> getProjectSuggestions(int count, int workspaceId)
            {
                for (int i = 0; i < count; i++)
                {
                    var workspace = Substitute.For<IDatabaseWorkspace>();
                    workspace.Name.Returns($"Workspace{workspaceId}");
                    workspace.Id.Returns(workspaceId);
                    var project = Substitute.For<IDatabaseProject>();
                    project.Name.Returns($"Project{i}");
                    project.Workspace.Returns(workspace);
                    yield return new ProjectSuggestion(project);
                }
            }
            
            [Fact]
            public async Task IsClearedWhenTextIsChanged()
            {
                var oldSuggestions = getProjectSuggestions(3, 1);
                var newSuggestions = getProjectSuggestions(1, 1);
                var autocompleteProvider = Substitute.For<IAutocompleteProvider>();
                var queryText = "Query text";
                autocompleteProvider.Query(Arg.Any<string>(), Arg.Is(AutocompleteSuggestionType.Projects))
                    .Returns(Observable.Return(oldSuggestions));
                autocompleteProvider.Query(Arg.Is(queryText), Arg.Is(AutocompleteSuggestionType.Projects))
                    .Returns(Observable.Return(newSuggestions));
                DataSource.AutocompleteProvider.Returns(autocompleteProvider);
                await ViewModel.Initialize();

                ViewModel.Text = queryText;

                ViewModel.Suggestions.Should().HaveCount(1);
                ViewModel.Suggestions.First().Should().HaveCount(2);
            }

            [Fact]
            public async Task IsPopulatedAfterInitialization()
            {
                var projectSuggestions = getProjectSuggestions(10, 0);
                var suggestionsObservable = Observable.Return(projectSuggestions);
                var autocompleteProvider = Substitute.For<IAutocompleteProvider>();
                autocompleteProvider.Query(Arg.Any<string>(), Arg.Is(AutocompleteSuggestionType.Projects))
                    .Returns(suggestionsObservable);
                DataSource.AutocompleteProvider.Returns(autocompleteProvider);
                
                await ViewModel.Initialize();

                ViewModel.Suggestions.Should().HaveCount(1);
                ViewModel.Suggestions.First().Should().HaveCount(11);
            }

            [Fact]
            public async Task PrependsEmptyProjectToEveryGroup()
            {
                var suggestions = new List<ProjectSuggestion>();
                suggestions.AddRange(getProjectSuggestions(3, 0));
                suggestions.AddRange(getProjectSuggestions(4, 1));
                suggestions.AddRange(getProjectSuggestions(1, 10));
                suggestions.AddRange(getProjectSuggestions(10, 54));
                var suggestionsObservable = Observable.Return(suggestions);
                var autocompleteProvider = Substitute.For<IAutocompleteProvider>();
                autocompleteProvider.Query(Arg.Any<string>(), Arg.Is(AutocompleteSuggestionType.Projects))
                    .Returns(suggestionsObservable);

                await ViewModel.Initialize();

                foreach (var group in ViewModel.Suggestions)
                {
                    group.First().ProjectName.Should().Be(Resources.NoProject);
                }
            }

            [Fact]
            public async Task GroupsProjectsByWorkspace()
            {
                var suggestions = new List<ProjectSuggestion>();
                var workspaceIds = new[] { 0, 1, 10, 54 };
                suggestions.AddRange(getProjectSuggestions(3, workspaceIds[0]));
                suggestions.AddRange(getProjectSuggestions(4, workspaceIds[1]));
                suggestions.AddRange(getProjectSuggestions(1, workspaceIds[2]));
                suggestions.AddRange(getProjectSuggestions(10, workspaceIds[3]));
                var suggestionsObservable = Observable.Return(suggestions);
                var autocompleteProvider = Substitute.For<IAutocompleteProvider>();
                DataSource.AutocompleteProvider.Returns(autocompleteProvider);
                autocompleteProvider.Query(Arg.Any<string>(), Arg.Is(AutocompleteSuggestionType.Projects))
                    .Returns(suggestionsObservable);

                await ViewModel.Initialize();

                ViewModel.Suggestions.Should().HaveCount(4);
                foreach (var suggestionGroup in ViewModel.Suggestions)
                {
                    foreach (var suggestion in suggestionGroup)
                    {
                        if (suggestion.ProjectName == Resources.NoProject)
                            continue;
                        suggestion.Workspace.Should().Be(suggestionGroup.WorkspaceName);
                    }
                }
            }
        }
    }
}
