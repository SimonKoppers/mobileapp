using System;
using Foundation;
using MvvmCross.Binding.BindingContext;
using MvvmCross.Binding.iOS.Views;
using Toggl.Foundation.MvvmCross.Converters;
using Toggl.Foundation.MvvmCross.ViewModels;
using UIKit;

namespace Toggl.Daneel.Views
{
    public sealed partial class SelectableTagViewCell : MvxTableViewCell
    {
        public static readonly NSString Key = new NSString(nameof(SelectableTagViewCell));
        public static readonly UINib Nib;

        private static UIImage checkBoxCheckedImage = UIImage.FromBundle("icCheckBoxChecked");
        private static UIImage checkBoxUncheckedImage = UIImage.FromBundle("icCheckBoxUnchecked");

        static SelectableTagViewCell()
        {
            Nib = UINib.FromName(nameof(SelectableTagViewCell), NSBundle.MainBundle);
        }

        public SelectableTagViewCell(IntPtr handle) : base(handle)
        {
            // Note: this .ctor should not contain any initialization logic.
        }

        public override void AwakeFromNib()
        {
                base.AwakeFromNib();

            this.DelayBind(() =>
            {
                var checkboxImageConverter = new BoolToConstantValueConverter<UIImage>(
                    checkBoxCheckedImage, checkBoxUncheckedImage);

                var bindingSet = this.CreateBindingSet<SelectableTagViewCell, SelectableTagViewModel>();

                bindingSet.Bind(TagLabel).To(vm => vm.Name);

                bindingSet.Bind(SelectedImage)
                          .To(vm => vm.Selected)
                          .WithConversion(checkboxImageConverter);

                bindingSet.Apply();
            });
        }
    }
}
