using System;
using Foundation;
using MvvmCross.Binding.BindingContext;
using MvvmCross.Binding.iOS.Views;
using Toggl.Foundation.MvvmCross.Collections;
using Toggl.Foundation.MvvmCross.ViewModels;
using UIKit;

namespace Toggl.Daneel.Views
{
    public sealed partial class SelectTagsHeaderViewCell : MvxTableViewHeaderFooterView
    {
        public static readonly NSString Key = new NSString(nameof(SelectTagsHeaderViewCell));
        public static readonly UINib Nib;

        public bool TopSeparatorHidden
        {
            get => TopSeparator.Hidden;
            set => TopSeparator.Hidden = value;
        }

        static SelectTagsHeaderViewCell()
        {
            Nib = UINib.FromName(nameof(SelectTagsHeaderViewCell), NSBundle.MainBundle);
        }

        public SelectTagsHeaderViewCell(IntPtr handle) : base(handle)
        {
            // Note: this .ctor should not contain any initialization logic.
        }

        public override void AwakeFromNib()
        {
            base.AwakeFromNib();

            this.DelayBind(() =>
            {
                var bindingSet = this.CreateBindingSet<SelectTagsHeaderViewCell, WorkspaceGroupedCollection<SelectableTagViewModel>>();

                bindingSet.Bind(WorkspaceLabel).To(vm => vm.WorkspaceName);

                bindingSet.Apply();
            });
        }
    }
}
