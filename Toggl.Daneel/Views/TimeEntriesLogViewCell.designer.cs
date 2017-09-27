// WARNING
//
// This file has been generated automatically by Visual Studio from the outlets and
// actions declared in your storyboard file.
// Manual changes to this file will not be maintained.
//
using Foundation;
using System;
using System.CodeDom.Compiler;

namespace Toggl.Daneel.Views
{
    [Register ("TimeEntriesLogViewCell")]
    partial class TimeEntriesLogViewCell
    {
        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIButton ContinueButton { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIImageView ContinueImageView { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UILabel DescriptionLabel { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.NSLayoutConstraint DescriptionTopDistanceConstraint { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UILabel ProjectTaskClientLabel { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIImageView SyncErrorImageView { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UILabel TimeLabel { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIImageView UnsyncedImageView { get; set; }

        void ReleaseDesignerOutlets ()
        {
            if (ContinueButton != null) {
                ContinueButton.Dispose ();
                ContinueButton = null;
            }

            if (ContinueImageView != null) {
                ContinueImageView.Dispose ();
                ContinueImageView = null;
            }

            if (DescriptionLabel != null) {
                DescriptionLabel.Dispose ();
                DescriptionLabel = null;
            }

            if (DescriptionTopDistanceConstraint != null) {
                DescriptionTopDistanceConstraint.Dispose ();
                DescriptionTopDistanceConstraint = null;
            }

            if (ProjectTaskClientLabel != null) {
                ProjectTaskClientLabel.Dispose ();
                ProjectTaskClientLabel = null;
            }

            if (SyncErrorImageView != null) {
                SyncErrorImageView.Dispose ();
                SyncErrorImageView = null;
            }

            if (TimeLabel != null) {
                TimeLabel.Dispose ();
                TimeLabel = null;
            }

            if (UnsyncedImageView != null) {
                UnsyncedImageView.Dispose ();
                UnsyncedImageView = null;
            }
        }
    }
}