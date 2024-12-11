using System.Collections.Generic;
using Volo.Abp.DependencyInjection;

namespace CAServer.ScheduledTask;

public interface IScheduledTaskManager
{
    public List<IScheduledTask> ScheduledTasks { get; }
    public void Add(IScheduledTask task);
}

public class ScheduledTaskManager : IScheduledTaskManager, ISingletonDependency
{
    public List<IScheduledTask> ScheduledTasks { get; set; } = new();

    public void Add(IScheduledTask task)
    {
        ScheduledTasks.Add(task);
    }
}