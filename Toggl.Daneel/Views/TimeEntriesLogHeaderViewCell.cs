using System;
using Foundation;
using MvvmCross.Binding.BindingContext;
using MvvmCross.Binding.iOS.Views;
using MvvmCross.Plugins.Visibility;
using Toggl.Daneel.Extensions;
using Toggl.Foundation.MvvmCross.Collections;
using Toggl.Foundation.MvvmCross.Converters;
using Toggl.Foundation.MvvmCross.ViewModels;
using UIKit;

namespace Toggl.Daneel.Views
{
    public partial class TimeEntriesLogHeaderViewCell : MvxTableViewHeaderFooterView
    {
        public static readonly NSString Key = new NSString(nameof(TimeEntriesLogHeaderViewCell));
        public static readonly UINib Nib;

        static TimeEntriesLogHeaderViewCell()
        {
            Nib = UINib.FromName(nameof(TimeEntriesLogHeaderViewCell), NSBundle.MainBundle);
        }

        protected TimeEntriesLogHeaderViewCell(IntPtr handle)
            : base(handle)
        {
        }

        public override void AwakeFromNib()
        {
            base.AwakeFromNib();

            ContentView.BackgroundColor = UIColor.White;
            DurationLabel.Font = DurationLabel.Font.GetMonospacedDigitFont();

            this.DelayBind(() =>
            {
                var visibilityConverter = new MvxVisibilityValueConverter();
                var timeSpanConverter = new TimeSpanToDurationValueConverter();
                var dateTitleConverter = new DateToTitleStringValueConverter();

                var bindingSet = this.CreateBindingSet<TimeEntriesLogHeaderViewCell, TimeEntryViewModelCollection>();

                //Text
                bindingSet.Bind(DateLabel)
                          .To(vm => vm.Date)
                          .WithConversion(dateTitleConverter);

                bindingSet.Bind(DurationLabel)
                          .To(vm => vm.TotalTime)
                          .WithConversion(timeSpanConverter);

                bindingSet.Apply();
            });
        }
    }
}
