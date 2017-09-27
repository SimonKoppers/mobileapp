using MvvmCross.Platform.UI;

namespace Toggl.Foundation.MvvmCross.Helper
{
    public static class Color
    {
        private static readonly MvxColor lightishGreen = new MvxColor(76, 217, 100);
        private static readonly MvxColor steel = new MvxColor(142, 142, 147);
        private static readonly MvxColor darkMint = new MvxColor(76, 190, 100);
        private static readonly MvxColor pinkishGrey = new MvxColor(206, 206, 206);
        private static readonly MvxColor black = new MvxColor(46, 46, 46);

        internal static class Onboarding
        {
            internal static readonly MvxColor TrackPageBorderColor = new MvxColor(14, 150, 213);
            internal static readonly MvxColor TrackPageBackgroundColor = new MvxColor(6, 170, 245);

            internal static readonly MvxColor LogPageBorderColor = new MvxColor(165, 81, 220);
            internal static readonly MvxColor LogPageBackgroundColor = new MvxColor(187, 103, 241);

            internal static readonly MvxColor SummaryPageBorderColor = new MvxColor(230, 179, 31);
            internal static readonly MvxColor SummaryPageBackgroundColor = new MvxColor(241, 195, 63);

            internal static readonly MvxColor LoginPageBackgroundColor = new MvxColor(219, 40, 46);
        }

        public static class NavigationBar
        {
            public static readonly MvxColor BackButton = new MvxColor(94, 91, 91);
            public static readonly MvxColor BackgroundColor = new MvxColor(250, 251, 252);
        }

        public static class Main
        {
            public static readonly MvxColor CardBorder = new MvxColor(232, 232, 232);

            public static readonly MvxColor Syncing = new MvxColor(181, 188, 192);
            public static readonly MvxColor SyncCompleted = lightishGreen;
        }

        public static class TimeEntriesLog
        {
            public static readonly MvxColor ClientColor = new MvxColor(163, 163, 163);

            public static readonly MvxColor ButtonBorder = new MvxColor(243, 243, 243);

            public static readonly MvxColor SectionFooter = new MvxColor(250, 251, 252);
        }

        public static class StartTimeEntry
        {
            public static readonly MvxColor Cursor = lightishGreen;

            public static readonly MvxColor Placeholder = pinkishGrey;

            public static readonly MvxColor ActiveButton = lightishGreen;

            public static readonly MvxColor BoldQuerySuggestionColor = new MvxColor(181, 188, 192);

            public static readonly MvxColor InactiveButton = new MvxColor(181, 188, 192);

            public static readonly MvxColor SeparatorColor = new MvxColor(181, 188, 192);

            public static readonly MvxColor ProjectTokenBorder = new MvxColor(232, 232, 232);
        }

        public static class EditTimeEntry
        {
            public static readonly MvxColor ClientText = new MvxColor(94, 91, 91);
        }

        public static class ModalDialog
        {
            public static readonly MvxColor BackgroundOverlay = new MvxColor(181, 188, 192);
        }
    }
}
