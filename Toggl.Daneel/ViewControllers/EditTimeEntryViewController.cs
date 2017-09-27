using CoreGraphics;
using Foundation;
using MvvmCross.Binding.BindingContext;
using MvvmCross.Binding.iOS;
using MvvmCross.iOS.Views;
using MvvmCross.Plugins.Color.iOS;
using MvvmCross.Plugins.Visibility;
using Toggl.Daneel.Combiners;
using Toggl.Daneel.Extensions;
using Toggl.Daneel.Presentation.Attributes;
using Toggl.Foundation;
using Toggl.Foundation.MvvmCross.Converters;
using Toggl.Foundation.MvvmCross.Helper;
using Toggl.Foundation.MvvmCross.ViewModels;
using UIKit;

namespace Toggl.Daneel.ViewControllers
{
    [ModalCardPresentation]
    public partial class EditTimeEntryViewController : MvxViewController
    {
        private const int switchHeight = 24;

        public EditTimeEntryViewController() : base(nameof(EditTimeEntryViewController), null)
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            prepareViews();

            var durationConverter = new TimeSpanToDurationWithUnitValueConverter();
            var dateConverter = new DateToTitleStringValueConverter();
            var timeConverter = new DateTimeToTimeConverter();
            var visibilityConverter = new MvxVisibilityValueConverter();
            var inverterVisibilityConverter = new MvxInvertedVisibilityValueConverter();
            var projectTaskClientCombiner = new ProjectTaskClientValueCombiner(
                ProjectTaskClientLabel.Font.CapHeight,
                Color.EditTimeEntry.ClientText.ToNativeColor(),
                false
            );

            var bindingSet = this.CreateBindingSet<EditTimeEntryViewController, EditTimeEntryViewModel>();
            
            //Text
            bindingSet.Bind(DescriptionTextField).To(vm => vm.Description);
            bindingSet.Bind(BillableSwitch).To(vm => vm.Billable);
            bindingSet.Bind(DurationLabel)
                      .To(vm => vm.Duration)
                      .WithConversion(durationConverter);
            
            bindingSet.Bind(ProjectTaskClientLabel)
                      .For(v => v.AttributedText)
                      .ByCombining(projectTaskClientCombiner,
                          v => v.Project,
                          v => v.Task,
                          v => v.Client,
                          v => v.ProjectColor); 

            bindingSet.Bind(StartDateLabel)
                      .To(vm => vm.StartTime)
                      .WithConversion(dateConverter);
            
            bindingSet.Bind(StartTimeLabel)
                      .To(vm => vm.StartTime)
                      .WithConversion(timeConverter);
            
            //Commands
            bindingSet.Bind(CloseButton).To(vm => vm.CloseCommand);
            bindingSet.Bind(DeleteButton).To(vm => vm.DeleteCommand);
            bindingSet.Bind(ConfirmButton).To(vm => vm.ConfirmCommand);
            bindingSet.Bind(DurationLabel)
                      .For(v => v.BindTap())
                      .To(vm => vm.EditDurationCommand);
            
            bindingSet.Bind(ProjectTaskClientLabel)
                      .For(v => v.BindTap())
                      .To(vm => vm.SelectProjectCommand);

            bindingSet.Bind(AddProjectAndTaskView)
                      .For(v => v.BindTap())
                      .To(vm => vm.SelectProjectCommand);
            
            bindingSet.Bind(StartDateTimeView)
                      .For(v => v.BindTap())
                      .To(vm => vm.SelectStartDateTimeCommand);

            //Project visibility
            bindingSet.Bind(AddProjectAndTaskView)
                      .For(v => v.BindVisible())
                      .To(vm => vm.Project)
                      .WithConversion(visibilityConverter);
            
            bindingSet.Bind(ProjectTaskClientLabel)
                      .For(v => v.BindVisible())
                      .To(vm => vm.Project)
                      .WithConversion(inverterVisibilityConverter);
            
            //Tags visibility
            bindingSet.Bind(AddTagsView)
                      .For(v => v.BindVisible())
                      .To(vm => vm.Tags)
                      .WithConversion(visibilityConverter);
            
            bindingSet.Bind(TagsLabel)
                      .For(v => v.BindVisible())
                      .To(vm => vm.Tags)
                      .WithConversion(inverterVisibilityConverter);
            
            bindingSet.Apply();
        }

        private void prepareViews()
        {
            DurationLabel.Font = DurationLabel.Font.GetMonospacedDigitFont();
            PreferredContentSize = View.Frame.Size;
            resizeSwitch();
            prepareDescriptionField();
        }

        private void resizeSwitch()
        {
            var scale = switchHeight / BillableSwitch.Frame.Height;
            BillableSwitch.Transform = CGAffineTransform.MakeScale(scale, scale);
        }

        private void prepareDescriptionField()
        {
            var placeholderAttributes = new UIStringAttributes
            {
                //This should be the same as Color.pinkishGrey (206, 206, 206),
                //but iOS makes the color a bit darker, when applied to
                //UITextField.AttributedPlaceholder, so this is made a bit
                //lighter than the actual color.
                ForegroundColor = UIColor.FromRGB(215, 215, 215)
            };
            DescriptionTextField.AttributedPlaceholder = new NSAttributedString(Resources.AddDescription, placeholderAttributes);

            DescriptionTextField.ShouldReturn += (textField) =>
            {
                DescriptionTextField.ResignFirstResponder();
                return true; 
            };
        }
    }
}
