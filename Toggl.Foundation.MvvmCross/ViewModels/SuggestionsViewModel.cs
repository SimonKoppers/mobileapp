﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using MvvmCross.Core.ViewModels;
using PropertyChanged;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Suggestions;
using Toggl.Multivac;

namespace Toggl.Foundation.MvvmCross.ViewModels
{
    [Preserve(AllMembers = true)]
    public sealed class SuggestionsViewModel : MvxViewModel
    {
        private readonly ITogglDataSource dataSource;
        private readonly ISuggestionProviderContainer suggestionProviders;

        private IDisposable emptyDatabaseDisposable;

        public MvxObservableCollection<Suggestion> Suggestions { get; }
            = new MvxObservableCollection<Suggestion>();

        [DependsOn(nameof(Suggestions))]
        public bool IsEmpty => !Suggestions.Any();

        public SuggestionsViewModel(ITogglDataSource dataSource, ISuggestionProviderContainer suggestionProviders)
        {
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            Ensure.Argument.IsNotNull(suggestionProviders, nameof(suggestionProviders));

            this.dataSource = dataSource;
            this.suggestionProviders = suggestionProviders;
        }

        public async override Task Initialize()
        {
            await base.Initialize();

            emptyDatabaseDisposable = dataSource.TimeEntries.IsEmpty
                .DistinctUntilChanged()
                .Subscribe(fetchSuggestions);
        }

        private void fetchSuggestions(bool databaseIsEmpty)
        {
            Suggestions.Clear();

            if (databaseIsEmpty) return;

            suggestionProviders
                .Providers
                .Select(provider => provider.GetSuggestions())
                .Aggregate(Observable.Merge)
                .Subscribe(addSuggestions);
        }

        private void addSuggestions(Suggestion suggestions)
        {
            Suggestions.Add(suggestions);
            RaisePropertyChanged(nameof(IsEmpty));
        }
    }
}
