using System;
using Toggl.Multivac.Models;
using Toggl.PrimeRadiant;

namespace Toggl.Foundation.Tests.Sync.States
{
    public sealed class TestModel : IBaseModel, IDatabaseSyncable
    {
        public long Id { get; set; }

        public SyncStatus SyncStatus { get; set; }

        public string LastSyncErrorMessage { get; set; }

        public bool IsDeleted { get; set; }

        public TestModel(long id, SyncStatus status, bool deleted = false)
        {
            Id = id;
            SyncStatus = status;
            IsDeleted = deleted;
        }

        public static TestModel Dirty(long id)
            => new TestModel(id, SyncStatus.SyncNeeded);

        public static TestModel DirtyDeleted(long id)
            => new TestModel(id, SyncStatus.SyncNeeded, true);
    }
}
