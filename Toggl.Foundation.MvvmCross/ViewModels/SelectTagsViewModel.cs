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
using Toggl.Multivac;
using Toggl.Multivac.Extensions;

namespace Toggl.Foundation.MvvmCross.ViewModels
{
    [Preserve(AllMembers = true)]
    public sealed class SelectTagsViewModel : MvxViewModel<long[], long[]>
    {
        private readonly IMvxNavigationService navigationService;
        private readonly ITogglDataSource dataSource;
        private readonly Subject<string> textSubject = new Subject<string>();
        private readonly HashSet<long> selectedTagIds = new HashSet<long>();

        private long[] defaultResult;

        public string Text { get; set; } = "";

        public MvxObservableCollection<SelectableTagCollection> Tags { get; }
            = new MvxObservableCollection<SelectableTagCollection>();

        public IMvxAsyncCommand CloseCommand { get; }

        public IMvxAsyncCommand SaveCommand { get; }

        public IMvxCommand<SelectableTag> SelectTagCommand { get; }

        public SelectTagsViewModel(ITogglDataSource dataSource, IMvxNavigationService navigationService)
        {
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            Ensure.Argument.IsNotNull(navigationService, nameof(navigationService));

            this.dataSource = dataSource;
            this.navigationService = navigationService;

            CloseCommand = new MvxAsyncCommand(close);
            SaveCommand = new MvxAsyncCommand(save);
            SelectTagCommand = new MvxCommand<SelectableTag>(selectTag);
        }
        
        public override void Prepare(long[] parameter)
        {
            defaultResult = parameter;
            foreach (var tagId in parameter)
                selectedTagIds.Add(tagId);
        }

        public override async Task Initialize()
        {
            await base.Initialize();

            textSubject.AsObservable()
                       .StartWith(Text)
                       .SelectMany(text => dataSource.AutocompleteProvider.Query(text, AutocompleteSuggestionType.Tags))
                       .Select(suggestions => suggestions.Cast<TagSuggestion>())
                       .Subscribe(onTags);
        }

        private void OnTextChanged()
        {
            textSubject.OnNext(Text);
        }

        private void onTags(IEnumerable<TagSuggestion> tags)
        {
            Tags.Clear();

            tags.Select(createSelectableTag)
                .GroupBy(tag => tag.Workspace)
                .Select(grouping => new SelectableTagCollection(grouping.Key, grouping))
                .ForEach(Tags.Add);
        }

        private SelectableTag createSelectableTag(TagSuggestion tagSuggestion)
            => new SelectableTag(tagSuggestion, selectedTagIds.Contains(tagSuggestion.TagId));

        private Task close()
            => navigationService.Close(this, defaultResult);

        private Task save() => navigationService.Close(this, selectedTagIds.ToArray());

        private void selectTag(SelectableTag tag)
        {
            tag.Selected = !tag.Selected;

            if (tag.Selected)
                selectedTagIds.Add(tag.Id);
            else
                selectedTagIds.Remove(tag.Id);
        }
    }
}
