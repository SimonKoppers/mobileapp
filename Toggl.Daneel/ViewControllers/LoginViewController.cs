﻿using CoreText;
using Foundation;
using MvvmCross.Binding.BindingContext;
using MvvmCross.Binding.iOS;
using MvvmCross.iOS.Views;
using MvvmCross.iOS.Views.Presenters.Attributes;
using Toggl.Daneel.Extensions;
using Toggl.Foundation;
using Toggl.Foundation.MvvmCross.Helper;
using Toggl.Foundation.MvvmCross.ViewModels;
using UIKit;

namespace Toggl.Daneel.ViewControllers
{
    [MvxChildPresentation]
    public partial class LoginViewController : MvxViewController<LoginViewModel>
    {
        private const int forgotPasswordLabelOffset = 27;

        private readonly UIBarButtonItem backButton =
            new UIBarButtonItem { Title = Resources.LoginBackButton, TintColor = UIColor.White };
        
        private readonly UIBarButtonItem nextButton =
            new UIBarButtonItem { Title = Resources.LoginNextButton, TintColor = UIColor.White };
    
        public LoginViewController() 
            : base(nameof(LoginViewController), null)
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
  
            Title = ViewModel.Title;

            prepareTextFields();
            prepareNavigationBar();

            UIKeyboard.Notifications.ObserveWillShow(keyboardWillShow);
            UIKeyboard.Notifications.ObserveWillHide(keyboardWillHide);
            
            var bindingSet = this.CreateBindingSet<LoginViewController, LoginViewModel>();

            //Text
            bindingSet.Bind(EmailTextField).To(vm => vm.Email);
            bindingSet.Bind(PasswordTextField).To(vm => vm.Password);
            bindingSet.Bind(ErrorLabel).To(vm => vm.ErrorText);
            bindingSet.Bind(PasswordTextField)
                      .For(v => v.BindSecureTextEntry())
                      .To(vm => vm.IsPasswordMasked);

            //Commands
            bindingSet.Bind(backButton)
                      .For(v => v.BindCommand())
                      .To(vm => vm.BackCommand);

            bindingSet.Bind(nextButton)
                      .For(v => v.BindCommand())
                      .To(vm => vm.NextCommand);

            bindingSet.Bind(ShowPasswordButton)
                      .For(v => v.BindTap())
                      .To(vm => vm.TogglePasswordVisibilityCommand);

            bindingSet.Bind(PasswordManagerButton)
                      .For(v => v.BindTap())
                      .To(vm => vm.StartPasswordManagerCommand);

            //Enabled
            bindingSet.Bind(EmailTextField)
                      .For(v => v.BindShouldReturn())
                      .To(vm => vm.NextCommand);
            
            bindingSet.Bind(PasswordTextField)
                      .For(v => v.BindShouldReturn())
                      .To(vm => vm.NextCommand);

            bindingSet.Bind(nextButton)
                      .For(v => v.BindAnimatedEnabled())
                      .To(vm => vm.NextIsEnabled);

            //Visibility
            bindingSet.Bind(EmailTextField)
                      .For(v => v.BindAnimatedVisibility())
                      .To(vm => vm.IsEmailPage);

            bindingSet.Bind(PasswordTextField)
                      .For(v => v.BindAnimatedVisibility())
                      .To(vm => vm.IsPasswordPage);

            bindingSet.Bind(ShowPasswordButton)
                      .For(v => v.BindAnimatedVisibility())
                      .To(vm => vm.ShowPasswordButtonVisible);

            bindingSet.Bind(ErrorLabel)
                      .For(v => v.BindAnimatedVisibility())
                      .To(vm => vm.HasError);

            bindingSet.Bind(ActivityIndicator)
                      .For(v => v.BindVisible())
                      .To(vm => vm.IsLoading);

            if (ViewModel.IsPasswordManagerAvailable)
            {
                bindingSet.Bind(PasswordManagerButton)
                          .For(v => v.BindAnimatedVisibility())
                          .To(vm => vm.IsEmailPage);
            }
            else
            {
                PasswordManagerButton.Hidden = true;
            }

            //State
            bindingSet.Bind(EmailTextField)
                      .For(v => v.BindFocus())
                      .To(vm => vm.IsEmailPage);
            
            bindingSet.Bind(PasswordTextField)
                      .For(v => v.BindFocus())
                      .To(vm => vm.IsPasswordPage);

            bindingSet.Apply();

            EmailTextField.BecomeFirstResponder();
        }

        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);
            NavigationController.NavigationBar.UserInteractionEnabled = true;
        }

        private void keyboardWillShow(object sender, UIKeyboardEventArgs e)
        {
            BottomConstraint.Constant = e.FrameBegin.Height + forgotPasswordLabelOffset;
            UIView.Animate(Animation.Timings.EnterTiming, () => View.LayoutIfNeeded());
        }

        private void keyboardWillHide(object sender, UIKeyboardEventArgs e)
        {
            BottomConstraint.Constant = forgotPasswordLabelOffset;
            UIView.Animate(Animation.Timings.EnterTiming, () => View.LayoutIfNeeded());
        }

        private void prepareTextFields()
        {
            var stringAttributes = new CTStringAttributes(
                new UIStringAttributes { ForegroundColor = UIColor.White.ColorWithAlpha(0.5f) }.Dictionary
            );

            EmailTextField.TintColor = UIColor.White;
            EmailTextField.AttributedPlaceholder = 
                new NSAttributedString(Resources.LoginSignUpEmailPlaceholder, stringAttributes);

            PasswordTextField.TintColor = UIColor.White;
            PasswordTextField.AttributedPlaceholder = 
                new NSAttributedString(Resources.LoginSignUpPasswordPlaceholder, stringAttributes);

            ForgotPasswordButton.SetTitle(Resources.LoginForgotPassword, UIControlState.Normal);
        }

        private void prepareNavigationBar()
        {
            var attributes = new UITextAttributes { Font = UIFont.SystemFontOfSize(14, UIFontWeight.Medium) };
            var spaceFix = new UIBarButtonItem(UIBarButtonSystemItem.FixedSpace) { Width = 8 };

            NavigationItem.LeftBarButtonItems = new[] { spaceFix, backButton };
            NavigationItem.RightBarButtonItems = new[] { spaceFix, nextButton };

            backButton.SetTitleTextAttributes(attributes, UIControlState.Normal);
            nextButton.SetTitleTextAttributes(attributes, UIControlState.Normal);
            backButton.SetTitleTextAttributes(attributes, UIControlState.Disabled);
            nextButton.SetTitleTextAttributes(attributes, UIControlState.Disabled);
        }
    }
}

