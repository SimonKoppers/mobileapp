using System.Linq;
using System.Text;
using CoreGraphics;
using Foundation;
using MvvmCross.Platform.UI;
using MvvmCross.Plugins.Color.iOS;
using Toggl.Daneel.Views;
using Toggl.Foundation.Autocomplete;
using Toggl.Foundation.MvvmCross.Helper;
using UIKit;

namespace Toggl.Daneel.Extensions
{
    public static class TextExtensions
    {
        private const string space = "\u00A0";
        private static readonly NSParagraphStyle paragraphStyle;
        private static readonly UIColor strokeColor = Color.StartTimeEntry.ProjectTokenBorder.ToNativeColor();

        static TextExtensions()
        {
            paragraphStyle = new NSMutableParagraphStyle
            {
                MinimumLineHeight = 24,
                MaximumLineHeight = 24
            };
        }

        public static string GetDescription(this UITextView self, TextFieldInfo info)
        {
            var result = self.Text;

            var projectText = info.PaddedProjectName();
            if (!string.IsNullOrEmpty(projectText))
                result = result.Replace(projectText, "");

            foreach (var tag in info.Tags)
                result = result.Replace(PaddedTagName(tag.Name), "");
           
            return result;
        }

        public static string PaddedProjectName(this TextFieldInfo self)
            => string.IsNullOrEmpty(self.ProjectName) ? "" : $"      {self.ProjectName}   ".Replace(" ", space);

        public static string TagsText(this TextFieldInfo self)
            => self.Tags.Length == 0 ? "" :
                   self.Tags.Aggregate(
                       new StringBuilder(""), 
                       (builder, tag) => builder.Append(PaddedTagName(tag.Name)))
                   .ToString()
                   .Replace(" ", space);

        public static string PaddedTagName(string tagName)
            => $"   {tagName}   ".Replace(" ", space);

        public static NSAttributedString GetAttributedText(this TextFieldInfo self)
        {
            var projectName = self.PaddedProjectName();
            var tags = self.TagsText();
            var fullText = $"{self.Text}{projectName}{tags}";
            var result = new NSMutableAttributedString(fullText);
            var baselineOffset = string.IsNullOrEmpty(self.Text) ? 5 : 3;

            result.AddAttributes(new UIStringAttributes
            {
                ParagraphStyle = paragraphStyle,
                Font = UIFont.SystemFontOfSize(16, UIFontWeight.Regular),
            }, new NSRange(0, self.Text.Length));

            if (!string.IsNullOrEmpty(self.ProjectColor))
            {
                var color = MvxColor.ParseHexString(self.ProjectColor).ToNativeColor();

                var attributes = new UIStringAttributes
                {
                    ForegroundColor = color,
                    StrokeColor = strokeColor,
                    BaselineOffset = baselineOffset,
                    ParagraphStyle = paragraphStyle,
                    Font = UIFont.SystemFontOfSize(12, UIFontWeight.Regular),
                };
                attributes.Dictionary[TimeEntryTagsTextView.RoundedBackground] = color.ColorWithAlpha(0.12f);

                result.AddAttributes(attributes, new NSRange(self.Text.Length, projectName.Length));
            }

            if (!string.IsNullOrEmpty(tags))
            {
                var startingPosition = self.Text.Length + projectName.Length;
                
                for (int i = 0; i < self.Tags.Length; i++)
                {
                    var tagLength = self.Tags[i].Name.Length + 6;

                    var attributes = new UIStringAttributes
                    {
                        BaselineOffset = baselineOffset,
                        ParagraphStyle = paragraphStyle,
                        Font = UIFont.SystemFontOfSize(12, UIFontWeight.Regular),
                    };
                    attributes.Dictionary[TimeEntryTagsTextView.TagIndex] = new NSNumber(i);
                    attributes.Dictionary[TimeEntryTagsTextView.RoundedBorders] = strokeColor;
                    result.AddAttributes(attributes, new NSRange(startingPosition, tagLength));

                    startingPosition += tagLength;
                }
            }

            return result;
        }

        public static NSAttributedString EndingWithTick(this string self, double fontHeight)
        {
            var tick = GetAttachmentString("icDoneSmall", fontHeight);

            var range = new NSRange(0, 1);
            var attributes = new UIStringAttributes { ForegroundColor = UIColor.White };
            tick.AddAttributes(attributes, range);

            var result = new NSMutableAttributedString(self);
            result.Append(tick);

            return result;
        }

        public static NSMutableAttributedString GetAttachmentString(this string imageName, double fontHeight)
        {
            var attachment = new NSTextAttachment
            {
                Image = UIImage.FromBundle(imageName)
                               .ImageWithRenderingMode(UIImageRenderingMode.AlwaysTemplate)
            };

            var imageSize = attachment.Image.Size;
            var y = (fontHeight - imageSize.Height) / 2;
            attachment.Bounds = new CGRect(0, y, imageSize.Width, imageSize.Height);

            //There neeeds to be a space before the dot, otherwise the colors don't work
            var result = new NSMutableAttributedString(" ");
            var attachmentString = NSAttributedString.FromAttachment(attachment);
            result.Append(attachmentString);

            return result;
        }
    }
}
