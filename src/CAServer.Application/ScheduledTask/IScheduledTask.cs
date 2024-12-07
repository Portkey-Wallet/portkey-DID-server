using System;
using System.Threading.Tasks;
using CAServer.Commons;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.DistributedLocking;

namespace CAServer.ScheduledTask;

public abstract class ScheduledTaskBase : IScheduledTask
{
    public IAbpLazyServiceProvider LazyServiceProvider { get; set; }
    protected ILoggerFactory LoggerFactory => LazyServiceProvider.LazyGetRequiredService<ILoggerFactory>();

    protected ILogger Logger => LazyServiceProvider.LazyGetService<ILogger>(provider =>
        LoggerFactory?.CreateLogger(GetType().FullName) ?? NullLogger.Instance);

    private IAbpDistributedLock DistributedLock => LazyServiceProvider.LazyGetRequiredService<IAbpDistributedLock>();

    /// <summary>
    /// seconds
    /// </summary>
    protected int Period { get; set; } = 15;

    // The user can customize this attribute. 
    public string Corn
    {
        get => CronHelper.GetCronExpression(Period);
    }

    protected abstract Task DoWorkAsync();

    public async Task ExecuteAsync()
    {
        await using var handle = await DistributedLock.TryAcquireAsync(this.GetType().FullName);

        if (handle == null)
        {
            Logger.LogWarning("The worker is not finish. {0}", this.GetType().FullName);
            return;
        }

        try
        {
            Logger.LogInformation("The worker will execute. {0}", this.GetType().FullName);
            await DoWorkAsync();
            Logger.LogInformation("The worker execute finish. {0}", this.GetType().FullName);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "The worker execute error. {0}, errorMsg:{1}, stackTrace:{2}", this.GetType().FullName,
                e.Message, e.StackTrace ?? "-");
        }
    }
}

public interface IScheduledTask : ISingletonDependency
{
    string Corn { get; }
    Task ExecuteAsync();
}