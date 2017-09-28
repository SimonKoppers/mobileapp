using System;
using System.Collections.Generic;
using System.Linq;
using Toggl.Foundation.Autocomplete.Suggestions;
using Toggl.Multivac.Models;

namespace Toggl.Foundation.Autocomplete
{
    public struct TextFieldInfo : IEquatable<TextFieldInfo>
    {
        public static TextFieldInfo Empty { get; } = new TextFieldInfo("", 0, null, "", "", new TagSuggestion[0]);

        public string Text { get; }

        public int CursorPosition { get; }

        public long? ProjectId { get; }

        public string ProjectColor { get; }

        public string ProjectName { get; }

        public TagSuggestion[] Tags { get; }

        public int DescriptionCursorPosition
            => Math.Min(CursorPosition, Text.Length);

        private TextFieldInfo(
            string text, 
            int cursorPosition, 
            long? projectId, 
            string projectName, 
            string projectColor,
            TagSuggestion[] tags)
        {
            Text = text;
            Tags = tags;
            ProjectId = projectId;
            ProjectName = projectName;
            ProjectColor = projectColor;
            CursorPosition = cursorPosition;
        }

        public TextFieldInfo WithTextAndCursor(string text, int cursorPosition)
           => new TextFieldInfo(text, cursorPosition, ProjectId, ProjectName, ProjectColor, Tags);

        public TextFieldInfo WithProjectInfo(long id, string name, string color)
           => new TextFieldInfo(Text, CursorPosition, id, name, color, Tags);

        public TextFieldInfo RemoveProjectInfo()
            => new TextFieldInfo(Text, CursorPosition, null, "", "", Tags);


        public TextFieldInfo RemoveTagQueryFromDescriptionIfNeeded()
        {
            var indexOfTagQuerySymbol = Text.IndexOf(QuerySymbols.Tags);
            if (indexOfTagQuerySymbol < 0) return this;

            var newText = Text.Substring(0, indexOfTagQuerySymbol);
            return WithTextAndCursor(newText, newText.Length);
        }

        public TextFieldInfo RemoveProjectQueryFromDescriptionIfNeeded()
        {
            var indexOfProjectQuerySymbol = Text.IndexOf(QuerySymbols.Projects);
            if (indexOfProjectQuerySymbol < 0) return this;

            var newText = Text.Substring(0, indexOfProjectQuerySymbol);
            return WithTextAndCursor(newText, newText.Length);
        }

        public TextFieldInfo AddTag(TagSuggestion tagSuggestion)
        {
            var tags = new List<TagSuggestion>(Tags) { tagSuggestion }.ToArray();
            
            return new TextFieldInfo(Text, CursorPosition, ProjectId, ProjectName, ProjectColor, tags);
        }

        public TextFieldInfo RemoveTag(TagSuggestion tag)
        {
            var newTags = Tags.ToList();
            newTags.Remove(tag);
            return new TextFieldInfo(Text, CursorPosition, ProjectId, ProjectName, ProjectColor, newTags.ToArray());
        }
       
        public static bool operator ==(TextFieldInfo left, TextFieldInfo right)
            => left.Equals(right);

        public static bool operator !=(TextFieldInfo left, TextFieldInfo right)
            => !left.Equals(right);

        public override bool Equals(object obj)
            => obj is TextFieldInfo && Equals((TextFieldInfo)obj);

        public override int GetHashCode()
        {
            unchecked
            {
                return (Text.GetHashCode() * 397) ^
                       (CursorPosition.GetHashCode() * 397) ^
                       (ProjectColor.GetHashCode() * 397) ^
                       (Tags.Length.GetHashCode() * 397) ^
                       ProjectName.GetHashCode();
            }
        }

        public bool Equals(TextFieldInfo other)
            => Text == other.Text
            && CursorPosition == other.CursorPosition
            && ProjectName == other.ProjectName
            && ProjectColor == other.ProjectColor;
    }
}
