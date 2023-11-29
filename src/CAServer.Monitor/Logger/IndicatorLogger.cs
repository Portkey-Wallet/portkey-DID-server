using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Volo.Abp.DependencyInjection;

namespace CAServer.Monitor.Logger;

public interface IIndicatorLogger
{
    bool IsEnabled();
    void LogInformation(MonitorTag tag, string target, int latency);
    void LogInformation(InterIndicator interIndicator);
}

public class IndicatorLogger : IIndicatorLogger, ISingletonDependency
{
    private readonly IMonitorLogger _monitorLogger;
    private readonly IndicatorOptions _indicatorOptions;

    public IndicatorLogger(IMonitorLogger monitorLogger, IOptionsSnapshot<IndicatorOptions> indicatorOptions)
    {
        _monitorLogger = monitorLogger;
        _indicatorOptions = indicatorOptions.Value;
    }

    public bool IsEnabled() => _indicatorOptions.IsEnabled;

    public void LogInformation(InterIndicator interIndicator)
    {
        if (interIndicator == null) return;
        LogInformation(interIndicator.Tag, interIndicator.Target, interIndicator.Value);
    }

    public void LogInformation(MonitorTag tag, string target, int latency)
    {
        if (!_indicatorOptions.IsEnabled)
        {
            return;
        }

        LogInformation(GetIndicator(tag, target, latency));
    }

    private void LogInformation(Indicator indicator)
    {
        _monitorLogger.LogInformation(JsonConvert.SerializeObject(indicator, new JsonSerializerSettings()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        }));
    }

    private Indicator GetIndicator(MonitorTag tag, string target, int latency)
    {
        return new Indicator()
        {
            Application = _indicatorOptions.Application,
            Module = _indicatorOptions.Module,
            Tag = tag.ToString(),
            Target = target,
            Value = latency
        };
    }
}