using System;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;

namespace CAServer.ScheduledTask;

public static class ScheduledTaskExtension
{
    public static ApplicationInitializationContext AddWorker<TWorker>(
        this ApplicationInitializationContext context, CancellationToken cancellationToken = default)
        where TWorker : IScheduledTask
    {
        Check.NotNull(context, nameof(context));
        if (!typeof(TWorker).IsAssignableTo<IScheduledTask>())
        {
            throw new AbpException(
                $"Given type ({typeof(TWorker).AssemblyQualifiedName}) must implement the {typeof(IScheduledTask).AssemblyQualifiedName} interface, but it doesn't!");
        }

        var manager = context.ServiceProvider.GetRequiredService<IScheduledTaskManager>();
        manager.Add((IScheduledTask)context.ServiceProvider.GetRequiredService(typeof(TWorker)));

        return context;
    }
}