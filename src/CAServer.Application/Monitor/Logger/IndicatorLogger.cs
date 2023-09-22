using System;
using CAServer.Options;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Volo.Abp.DependencyInjection;

namespace CAServer.Monitor.Logger;

public interface IIndicatorLogger
{
    void LogInformation(MonitorTag tag, string target, int latency);
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

    public void LogInformation(MonitorTag tag, string target, int latency)
    {
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