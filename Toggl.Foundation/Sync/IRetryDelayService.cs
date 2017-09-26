using System;

namespace Toggl.Foundation.Sync
{
    public interface IRetryDelayService
    {
        TimeSpan NextSlowDelay();
        TimeSpan NextFastDelay();
        void Reset();
    }
}
