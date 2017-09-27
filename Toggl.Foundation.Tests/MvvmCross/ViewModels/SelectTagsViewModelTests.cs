using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FsCheck.Xunit;
using NSubstitute;
using Toggl.Foundation.Autocomplete;
using Toggl.Foundation.Autocomplete.Suggestions;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.Tests.Generators;
using Toggl.PrimeRadiant.Models;
using Xunit;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels
{
    public sealed class SelectTagsViewModelTests
    {
        public abstract class SelectTagsViewModelTest : BaseViewModelTests<SelectTagsViewModel>
        {
            protected override SelectTagsViewModel CreateViewModel()
                => new SelectTagsViewModel(DataSource, NavigationService);

            protected async Task EnsureClosesTheViewModel()
            {
                await ViewModel.CloseCommand.ExecuteAsync();

                await NavigationService.Received().Close(Arg.Is(ViewModel), Arg.Any<long[]>());
            }

            protected bool EnsureExpectedTagsAreReturned(long[] actual, long[] expected)
            {
                if (actual.Length != expected.Length) return false;

                foreach (var actualTag in actual)
                {
                    if (!expected.Contains(actualTag))
                        return false;
                }
                return true;
            }
        }

        public sealed class TheConstructor : SelectTagsViewModelTest
        {
            [Theory]
            [ClassData(typeof(TwoParameterConstructorTestData))]
            public void ThrowsIfAnyOfTheArgumentsIsNull(bool useDataSource, bool useNavigationService)
            {
                var dataSource = useDataSource ? DataSource : null;
                var navigationService = useNavigationService ? NavigationService : null;

                Action tryingToConstructWithEmptyParameters =
                    () => new SelectTagsViewModel(dataSource, navigationService);

                tryingToConstructWithEmptyParameters
                    .ShouldThrow<ArgumentNullException>();
            }
        }

        public sealed class TheCloseCommand : SelectTagsViewModelTest
        {
            [Fact]
            public Task ClosesTheViewModel()
                => EnsureClosesTheViewModel();

            [Fact]
            public async Task ReturnsTheSameTagsThatWerePassedToTheViewModel()
            {
                var tagids = new long[] { 1, 4, 29, 2 };
                ViewModel.Prepare(tagids);

                await ViewModel.CloseCommand.ExecuteAsync();

                await NavigationService
                    .Received()
                    .Close(Arg.Is(ViewModel), Arg.Is<long[]>(tagids));
            }
        }

        public sealed class TheSaveCommand : SelectTagsViewModelTest
        {
            [Fact]
            public Task ClosesTheViewModel()
                => EnsureClosesTheViewModel();

            [Fact]
            public async Task ReturnsTheSelectedTagIds()
            {
                var tagIds = Enumerable.Range(0, 20).Select(num => (long)num);
                var selectedTagIds = tagIds.Where(id => id % 2 == 0)
                    .ToArray();
                var selectedTags = selectedTagIds
                    .Select(createDatabaseTagSubstitute)
                    .Select(databaseTag => new TagSuggestion(databaseTag))
                    .Select(tagSuggestion => new SelectableTagViewModel(tagSuggestion, false));
                foreach (var tag in selectedTags)
                    ViewModel.SelectTagCommand.Execute(tag);

                await ViewModel.SaveCommand.ExecuteAsync();

                await NavigationService
                    .Received()
                    .Close(
                        Arg.Is(ViewModel),
                        Arg.Is<long[]>(ids => EnsureExpectedTagsAreReturned(ids, selectedTagIds))
                    );
            }

            [Fact]
            public async Task ReturnsEmptyArrayIfNoTagsWereSelected()
            {
                var expectedIds = new long[0];

                await ViewModel.SaveCommand.ExecuteAsync();

                await NavigationService
                    .Received()
                    .Close(
                        Arg.Is(ViewModel),
                        Arg.Is<long[]>(ids => EnsureExpectedTagsAreReturned(ids, expectedIds))
                    );
            }

            private IDatabaseTag createDatabaseTagSubstitute(long id)
            {
                var tag = Substitute.For<IDatabaseTag>();
                tag.Id.Returns(id);
                tag.Name.Returns($"Tag{id}");
                return tag;
            }
        }

        public sealed class TheTextProperty : SelectTagsViewModelTest
        {
            [Fact]
            public async Task WhenChangedQueriesTheAutocompleteProvider()
            {
                var text = "Some text";
                var autocompleteProvider = Substitute.For<IAutocompleteProvider>();
                DataSource.AutocompleteProvider.Returns(autocompleteProvider);
                await ViewModel.Initialize();

                ViewModel.Text = text;

                await autocompleteProvider.Received().Query(Arg.Is(text), Arg.Is(AutocompleteSuggestionType.Tags));
            }
        }

        public sealed class TheTagsProperty : SelectTagsViewModelTest
        {
            private IEnumerable<TagSuggestion> getTagSuggestions(int count, int workspaceId)
            {
                for (int i = 0; i < count; i++)
                {
                    var workspace = Substitute.For<IDatabaseWorkspace>();
                    workspace.Name.Returns($"Workspace{workspaceId}");
                    workspace.Id.Returns(workspaceId);
                    var tag = Substitute.For<IDatabaseTag>();
                    tag.Name.Returns($"Tag{i}");
                    tag.Workspace.Returns(workspace);
                    yield return new TagSuggestion(tag);
                }
            }

            [Fact]
            public async Task IsPopulatedAfterInitialization()
            {
                var tagSuggestions = getTagSuggestions(10, 0);
                var suggestionsObservable = Observable.Return(tagSuggestions);
                var autocompleteProvider = Substitute.For<IAutocompleteProvider>();
                autocompleteProvider.Query(Arg.Any<string>(), Arg.Is(AutocompleteSuggestionType.Tags))
                    .Returns(suggestionsObservable);
                DataSource.AutocompleteProvider.Returns(autocompleteProvider);

                await ViewModel.Initialize();

                ViewModel.Tags.Should().HaveCount(1);
                ViewModel.Tags.First().Should().HaveCount(10);
            }

            [Fact]
            public async Task IsGroupedByWorkspace()
            {
                var suggestions = new List<TagSuggestion>();
                var workspaceIds = new[] { 0, 1, 10, 54 };
                suggestions.AddRange(getTagSuggestions(3, workspaceIds[0]));
                suggestions.AddRange(getTagSuggestions(4, workspaceIds[1]));
                suggestions.AddRange(getTagSuggestions(1, workspaceIds[2]));
                suggestions.AddRange(getTagSuggestions(10, workspaceIds[3]));
                var suggestionsObservable = Observable.Return(suggestions);
                var autocompleteProvider = Substitute.For<IAutocompleteProvider>();
                DataSource.AutocompleteProvider.Returns(autocompleteProvider);
                autocompleteProvider.Query(Arg.Any<string>(), Arg.Is(AutocompleteSuggestionType.Tags))
                    .Returns(suggestionsObservable);

                await ViewModel.Initialize();

                ViewModel.Tags.Should().HaveCount(workspaceIds.Length);
                foreach (var tagGroup in ViewModel.Tags)
                {
                    foreach (var tag in tagGroup)
                    {
                        tag.Workspace.Should().Be(tagGroup.WorkspaceName);
                    }
                }
            }

            [Fact]
            public async Task IsClearedWhenTextIsChanged()
            {
                var oldSuggestions = getTagSuggestions(3, 1);
                var newSuggestions = getTagSuggestions(1, 1);
                var autocompleteProvider = Substitute.For<IAutocompleteProvider>();
                var queryText = "Query text";
                autocompleteProvider.Query(Arg.Any<string>(), Arg.Is(AutocompleteSuggestionType.Tags))
                    .Returns(Observable.Return(oldSuggestions));
                autocompleteProvider.Query(Arg.Is(queryText), Arg.Is(AutocompleteSuggestionType.Tags))
                    .Returns(Observable.Return(newSuggestions));
                DataSource.AutocompleteProvider.Returns(autocompleteProvider);
                await ViewModel.Initialize();

                ViewModel.Text = queryText;

                ViewModel.Tags.Should().HaveCount(1);
                ViewModel.Tags.First().Should().HaveCount(1);
            }
        }

        public sealed class TheSelectTagCommand : SelectTagsViewModelTest
        {
            private TagSuggestion tagSuggestion;

            public TheSelectTagCommand()
            {
                var databaseTag = Substitute.For<IDatabaseTag>();
                databaseTag.Name.Returns("Tag0");
                databaseTag.Id.Returns(0);
                tagSuggestion = new TagSuggestion(databaseTag);
            }

            [Property]
            public void SetsTheSelectedPropertyOnTheTagToTheOpposite(bool initialValue)
            {
                var selectableTag = new SelectableTagViewModel(tagSuggestion, initialValue);

                ViewModel.SelectTagCommand.Execute(selectableTag);

                selectableTag.Selected.Should().Be(!initialValue);
            }

            [Fact]
            public async Task AppendsTheTagIdToSelectedTagIdsIfNotSelectedAlready()
            {
                var selectableTag = new SelectableTagViewModel(tagSuggestion, false);

                ViewModel.SelectTagCommand.Execute(selectableTag);
                await ViewModel.SaveCommand.ExecuteAsync();

                await NavigationService
                    .Received()
                    .Close(
                        Arg.Is(ViewModel),
                        Arg.Is<long[]>(ids => EnsureExpectedTagsAreReturned(ids, new[] { selectableTag.Id }))
                    );
            }

            [Fact]
            public async Task RemovesTheTagIdFromSelectedTagIdsIfSelectedAlready()
            {
                var selectableTag = new SelectableTagViewModel(tagSuggestion, true);
                ViewModel.Prepare(new long[] { selectableTag.Id });

                ViewModel.SelectTagCommand.Execute(selectableTag);
                await ViewModel.SaveCommand.ExecuteAsync();

                await NavigationService
                    .Received()
                    .Close(
                        Arg.Is(ViewModel),
                        Arg.Is<long[]>(ids => EnsureExpectedTagsAreReturned(ids, new long[0]))
                    );
            }
        }

        public sealed class ThePrepareMethod : SelectTagsViewModelTest
        {
            [Fact]
            public async Task AddsAllPassedTagsToTheSelectedTags()
            {
                var tagIds = new long[] { 100, 3, 10, 34, 532 };

                ViewModel.Prepare(tagIds);
                await ViewModel.SaveCommand.ExecuteAsync();

                await NavigationService
                    .Received()
                    .Close(
                        Arg.Is(ViewModel),
                        Arg.Is<long[]>(ids => EnsureExpectedTagsAreReturned(ids, tagIds))
                    );
            }
        }
    }
}
