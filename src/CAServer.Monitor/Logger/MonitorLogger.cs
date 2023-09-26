using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace CAServer.Monitor.Logger;

public interface IMonitorLogger
{
    void LogInformation(string message);
}

public class MonitorLogger : IMonitorLogger, ISingletonDependency
{
    private readonly ILogger<MonitorLogger> _logger;
    public MonitorLogger(ILogger<MonitorLogger> logger)
    {
        _logger = logger;
    }

    public void LogInformation(string message)
    {
        _logger.LogInformation(message);
    }
}