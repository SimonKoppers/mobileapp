using System;
using System.Reactive;

namespace Toggl.Ultrawave.ApiClients
{
    public interface IStatusApi
    {
        IObservable<Unit> IsAvailable();
    }
}
