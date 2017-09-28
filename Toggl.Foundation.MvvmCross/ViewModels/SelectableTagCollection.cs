using System.Collections.Generic;
using System.Collections.ObjectModel;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;

namespace Toggl.Foundation.MvvmCross.ViewModels
{
    public sealed class SelectableTagCollection : ObservableCollection<SelectableTag>
    {
        public string Workspace { get; }

        public SelectableTagCollection(string workspace, IEnumerable<SelectableTag> tags)
        {
            Ensure.Argument.IsNotNull(workspace, nameof(workspace));
            Ensure.Argument.IsNotNull(tags, nameof(tags));

            Workspace = workspace;
            this.AddRange(tags);
        }
    }
}
