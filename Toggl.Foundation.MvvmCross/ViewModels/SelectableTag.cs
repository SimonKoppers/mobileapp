using MvvmCross.Core.ViewModels;
using Toggl.Foundation.Autocomplete.Suggestions;
using Toggl.Multivac;

namespace Toggl.Foundation.MvvmCross.ViewModels
{
    [Preserve(AllMembers = true)]
    public sealed class SelectableTag : MvxNotifyPropertyChanged
    {
        public long Id { get; }

        public string Name { get; }

        public string Workspace { get; }

        public bool Selected { get; set; }

        public SelectableTag(TagSuggestion tagSuggestion, bool selected)
        {
            Id = tagSuggestion.TagId;
            Name = tagSuggestion.Name;
            Workspace = tagSuggestion.Workspace;
            Selected = selected;
        }
}
}
