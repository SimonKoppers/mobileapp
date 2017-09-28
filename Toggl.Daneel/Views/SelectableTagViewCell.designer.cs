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
    [Register ("SelectableTagViewCell")]
    partial class SelectableTagViewCell
    {
        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIImageView SelectedImage { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UILabel TagLabel { get; set; }

        void ReleaseDesignerOutlets ()
        {
            if (SelectedImage != null) {
                SelectedImage.Dispose ();
                SelectedImage = null;
            }

            if (TagLabel != null) {
                TagLabel.Dispose ();
                TagLabel = null;
            }
        }
    }
}