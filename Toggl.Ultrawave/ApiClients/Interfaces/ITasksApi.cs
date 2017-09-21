using System;
using System.Collections.Generic;
using Toggl.Multivac.Models;

namespace Toggl.Ultrawave.ApiClients
{
    public interface ITasksApi
    {
        IObservable<List<ITask>> GetAll();
        IObservable<List<ITask>> GetAllSince(DateTimeOffset threshold);
        IObservable<ITask> Create(ITask task);
    }
}