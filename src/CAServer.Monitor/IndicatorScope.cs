using CAServer.Monitor.Commons;
using CAServer.Monitor.Logger;
using Volo.Abp.DependencyInjection;

namespace CAServer.Monitor;

public interface IIndicatorScope
{
    InterIndicator Begin(MonitorTag tag);
    InterIndicator Begin(MonitorTag tag, string target);
    void End(InterIndicator interIndicator);
    void End(string target, InterIndicator interIndicator);
}

public class IndicatorScope : IIndicatorScope, ISingletonDependency
{
    private readonly IIndicatorLogger _indicatorLogger;

    public IndicatorScope(IIndicatorLogger indicatorLogger)
    {
        _indicatorLogger = indicatorLogger;
    }

    public InterIndicator Begin(MonitorTag tag)
    {
        return Begin(tag, string.Empty);
    }

    public InterIndicator Begin(MonitorTag tag, string target)
    {
        if (!_indicatorLogger.IsEnabled()) return null;

        return new InterIndicator(tag, target);
    }

    public void End(InterIndicator interIndicator)
    {
        if (interIndicator == null) return;

        var duration = (int)(MonitorTimeHelper.GetTimeStampInMilliseconds() - interIndicator.StartTime);
        interIndicator.Value = duration;
        _indicatorLogger.LogInformation(interIndicator);
    }

    public void End(string target, InterIndicator interIndicator)
    {
        if (interIndicator == null) return;
        interIndicator.Target = target;

        End(interIndicator);
    }
}