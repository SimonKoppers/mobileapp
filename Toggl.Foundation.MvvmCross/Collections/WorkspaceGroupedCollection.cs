using System.Collections.Generic;
using MvvmCross.Core.ViewModels;
using Toggl.Multivac;

namespace Toggl.Foundation.MvvmCross.Collections
{
    public class WorkspaceGroupedCollection<T> : MvxObservableCollection<T>
    {
        public string WorkspaceName { get; }

        public WorkspaceGroupedCollection(string workspaceName, IEnumerable<T> items)
        {
            Ensure.Argument.IsNotNull(items, nameof(items));
            Ensure.Argument.IsNotNull(workspaceName, nameof(workspaceName));

            WorkspaceName = workspaceName;
            AddRange(items);
        }
    }
}
