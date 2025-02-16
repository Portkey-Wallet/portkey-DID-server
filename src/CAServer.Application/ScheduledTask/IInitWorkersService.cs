using System;
using System.Collections.Generic;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace CAServer.ScheduledTask;

public interface IInitWorkersService
{
    void InitRecurringWorkers();
}

public class InitWorkersService : IInitWorkersService, ISingletonDependency
{
    private readonly IRecurringJobManager _recurringJobs;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<InitWorkersService> _logger;

    public InitWorkersService(IRecurringJobManager recurringJobs,
        IServiceProvider serviceProvider, ILogger<InitWorkersService> logger)
    {
        _recurringJobs = recurringJobs;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public void InitRecurringWorkers()
    {
        var tasks = _serviceProvider.GetRequiredService<IScheduledTaskManager>();
        if (tasks == null || tasks.ScheduledTasks.IsNullOrEmpty())
        {
            _logger.LogWarning("There is no worker.");
        }
        
        foreach (var task in tasks.ScheduledTasks)
        {
            _logger.LogInformation("Add or update worker, name:{0}, corn:{1}", task.GetType().FullName, task.Corn);
            _recurringJobs.AddOrUpdate(task.GetType().FullName, () => task.ExecuteAsync(), task.Corn);
        }
    }
}