using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Foundation;
using MvvmCross.Binding.Bindings.SourceSteps;
using MvvmCross.Binding.Combiners;
using MvvmCross.Platform.UI;
using MvvmCross.Plugins.Color.iOS;
using Toggl.Daneel.Extensions;
using Toggl.Multivac;
using UIKit;

namespace Toggl.Daneel.Combiners
{
    public sealed class ProjectTaskClientValueCombiner : MvxFormatValueCombiner
    {
        private const string projectDotImageResource = "icProjectDot";
        private readonly UIColor clientColor;
        private readonly bool shouldColorProject;
        private readonly NSMutableAttributedString cachedDotString;

        public ProjectTaskClientValueCombiner(nfloat fontHeight, UIColor clientColor, bool shouldColorProject)
        {
            Ensure.Argument.IsNotNull(clientColor, nameof(clientColor));

            this.clientColor = clientColor;
            this.shouldColorProject = shouldColorProject;
            cachedDotString = projectDotImageResource.GetAttachmentString(fontHeight);
        }

        public override bool TryGetValue(IEnumerable<IMvxSourceStep> steps, out object value)
        {
            var stepList = steps.ToList();
            if (stepList.Count < 4)
                throw new ArgumentException($"{nameof(ProjectTaskClientValueCombiner)} needs at least 4 values to work - project, task, client and project color");

            var project = stepList[0].GetValue()?.ToString() ?? "";
            var task = stepList[1].GetValue()?.ToString() ?? "";
            var client = stepList[2].GetValue()?.ToString() ?? "";
            var projectColor = stepList[3].GetValue()?.ToString() ?? "";

            var color =
                string.IsNullOrEmpty(projectColor) ? null : MvxColor.ParseHexString(projectColor).ToNativeColor();

            var clone = new NSMutableAttributedString(cachedDotString);
            var dotString = tryAddColorToDot(clone, color);
            var projectInfo = buildString(project, task, client, color);
            dotString.Append(projectInfo);
            value = dotString;

            return true;
        }

        private static NSMutableAttributedString tryAddColorToDot(NSMutableAttributedString dotString, UIColor color)
        {
            if (color == null) return dotString;
            
            var range = new NSRange(0, 1);
            var attributes = new UIStringAttributes { ForegroundColor = color };
            dotString.AddAttributes(attributes, range);

            return dotString;
        }

        private NSAttributedString buildString(string project, string task, string client, UIColor color)
        {
            var builder = new StringBuilder();
            var hasClient = !string.IsNullOrEmpty(client);

            if (!string.IsNullOrEmpty(project))
                builder.Append($" {project}");
            
            if (!string.IsNullOrEmpty(task))
                builder.Append($": {task}");
            
            if (hasClient)
                builder.Append($" {client}");

            var text = builder.ToString();

            var result = new NSMutableAttributedString(text);
            var clientIndex = text.Length - client.Length;
            if (shouldColorProject)
            {
                var projectNameRange = new NSRange(0, clientIndex);
                var projectNameAttributes = new UIStringAttributes { ForegroundColor = color };
                result.AddAttributes(projectNameAttributes, projectNameRange);
            }

            if (!hasClient) return result;

            var clientNameRange = new NSRange(clientIndex, client.Length);
            var clientNameAttributes = new UIStringAttributes { ForegroundColor = clientColor };
            result.AddAttributes(clientNameAttributes, clientNameRange);

            return result;
        }
    }
}