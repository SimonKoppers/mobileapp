using System;
using MvvmCross.Core.ViewModels;
using Toggl.Multivac;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Models;

namespace Toggl.Foundation.MvvmCross.ViewModels
{
    [Preserve(AllMembers = true)]
    public sealed class TimeEntryViewModel : MvxNotifyPropertyChanged
    {
        public long Id { get; }

        public bool Billable { get; }

        public string Description { get; } = "";

        public long? ProjectId { get; }

        public string ProjectName { get; } = "";

        public string ProjectColor { get; } = "#00000000";

        public string ClientName { get; } = "";

        public string TaskName { get; } = "";

        public DateTimeOffset Start { get; }

        public TimeSpan Duration { get; }

        public bool HasProject { get; }

        public bool HasDescription { get; }

        public bool NeedsSync { get; }

        public bool CanSync { get; }

        public TimeEntryViewModel(IDatabaseTimeEntry timeEntry)
        {
            Ensure.Argument.IsNotNull(timeEntry, nameof(timeEntry));

            Id = timeEntry.Id;
            Start = timeEntry.Start;
            Billable = timeEntry.Billable;
            Description = timeEntry.Description;
            HasProject = timeEntry.Project != null;
            Duration = timeEntry.Stop.Value - Start;
            HasDescription = !string.IsNullOrEmpty(timeEntry.Description);

            CanSync = timeEntry.SyncStatus != SyncStatus.SyncFailed;
            NeedsSync = timeEntry.SyncStatus == SyncStatus.SyncNeeded;

            if (!HasProject) return;

            ProjectId = timeEntry.Project.Id;
            ProjectName = timeEntry.Project.Name;
            ProjectColor = timeEntry.Project.Color;

            ClientName = timeEntry.Project.Client?.Name ?? "";
        }
    }
}
