using System.Text;
using CAServer.Nightingale.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orleans.Runtime;
using Serilog;

namespace CAServer.Nightingale.Logging;

public class N9EClientForLogging : IN9EClient
{
    private readonly ILogger _logger;
    private readonly IOptionsMonitor<N9EClientForLoggingOptions> _n9EClientForLoggingOptions;
    private readonly string _hostName;
    private readonly IConfiguration? _configuration;

    public N9EClientForLogging(IServiceProvider serviceProvider,
        IOptionsMonitor<N9EClientForLoggingOptions> n9EClientForLoggingOptions)
    {
        _n9EClientForLoggingOptions = n9EClientForLoggingOptions;
        _logger = N9EClientLogHelper.CreateLogger(n9EClientForLoggingOptions);
        _hostName = HostHelper.GetLocalHostName();
        _configuration = serviceProvider.GetService<IConfiguration>();
    }

    public void TrackMetric(string name, double value, IDictionary<string, string>? properties = null)
    {
        TrackMetric(FormatMetric(name, value, properties));
    }

    public void TrackMetric(string name, TimeSpan value, IDictionary<string, string> properties = null)
    {
        TrackMetric(FormatMetric(name, value, properties));
    }

    public void IncrementMetric(string name)
    {
        TrackMetric(FormatMetric(name, 1, null));
    }

    public void IncrementMetric(string name, double value)
    {
        TrackMetric(FormatMetric(name, value, null));
    }

    public void DecrementMetric(string name)
    {
        TrackMetric(FormatMetric(name, -1, null));
    }

    public void DecrementMetric(string name, double value)
    {
        TrackMetric(FormatMetric(name, value * -1, null));
    }

    public void TrackTrace(string message)
    {
        //Doing nothing
    }

    public void TrackTrace(string message, Severity severity)
    {
        //Doing nothing
    }

    public void TrackTrace(string message, Severity severity, IDictionary<string, string> properties)
    {
        //Doing nothing
    }

    public void TrackTrace(string message, IDictionary<string, string> properties)
    {
        //Doing nothing
    }

    public void TrackRequest(string name, DateTimeOffset startTime, TimeSpan duration, string responseCode,
        bool success)
    {
        TrackMetric(FormatMetric(name, duration, new Dictionary<string, string>()
        {
            { N9EClientConstant.LabelStartTime, startTime.Millisecond.ToString() },
            { N9EClientConstant.LabelResponseCode, responseCode },
            { N9EClientConstant.LabelSuccess, success.ToString() }
        }));
    }

    public void TrackDependency(string dependencyName, string commandName, DateTimeOffset startTime, TimeSpan duration,
        bool success)
    {
        TrackMetric(FormatMetric(dependencyName, duration, new Dictionary<string, string>()
        {
            { N9EClientConstant.LabelCommandName, commandName },
            { N9EClientConstant.LabelStartTime, startTime.Millisecond.ToString() },
            { N9EClientConstant.LabelSuccess, success.ToString() }
        }));
    }

    public void TrackException(Exception exception, IDictionary<string, string> properties = null,
        IDictionary<string, double> metrics = null)
    {
        if (metrics != null && metrics.Count > 0)
        {
            foreach (var metric in metrics)
            {
                var dictionary = properties == null
                    ? new Dictionary<string, string>()
                    : new Dictionary<string, string>(properties);
                dictionary.Add(N9EClientConstant.LabelChart, metric.Key);
                dictionary.Add(N9EClientConstant.LabelMessage, exception.Message.ToString());
                TrackMetric(FormatMetric(N9EClientConstant.MetricExceptions, metric.Value, dictionary));
            }
        }
        else
        {
            var dictionary = properties == null
                ? new Dictionary<string, string>()
                : new Dictionary<string, string>(properties);
            dictionary.Add(N9EClientConstant.LabelMessage, exception.Message.ToString());
            TrackMetric(FormatMetric(N9EClientConstant.MetricExceptions, 1, dictionary));
        }
    }

    public void TrackEvent(string eventName, IDictionary<string, string> properties = null,
        IDictionary<string, double> metrics = null)
    {
        if (metrics != null && metrics.Count > 0)
        {
            foreach (var metric in metrics)
            {
                var dictionary = properties == null
                    ? new Dictionary<string, string>()
                    : new Dictionary<string, string>(properties);
                dictionary.Add(N9EClientConstant.LabelChart, metric.Key);
                TrackMetric(FormatMetric(eventName, metric.Value, dictionary));
            }
        }
        else
        {
            TrackMetric(FormatMetric(eventName, 1, properties));
        }
    }

    public bool IsEnabled()
    {
        return !_n9EClientForLoggingOptions.CurrentValue.DisableLogging;
    }

    public void Flush()
    {
    }

    public void Close()
    {
    }

    private string FormatMetric(string? name, int value, IDictionary<string, string>? properties)
    {
        return $"{FormatMetricName(name)}{FormatMetricLabel(properties)} {value}";
    }

    private string FormatMetric(string? name, long value, IDictionary<string, string>? properties)
    {
        return $"{FormatMetricName(name)}{FormatMetricLabel(properties)} {value}";
    }

    private string FormatMetric(string? name, double value, IDictionary<string, string>? properties)
    {
        return $"{FormatMetricName(name)}{FormatMetricLabel(properties)} {value}";
    }

    private string FormatMetric(string? name, TimeSpan value, IDictionary<string, string>? properties)
    {
        return $"{FormatMetricName(name)}{FormatMetricLabel(properties)} {value.Milliseconds}";
    }

    private string FormatMetricName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        var formatMetricName = name.Replace(".", "_");
        if (formatMetricName.Length > _n9EClientForLoggingOptions.CurrentValue.MetricNameMaxLength)
        {
            formatMetricName = formatMetricName[.._n9EClientForLoggingOptions.CurrentValue.MetricNameMaxLength];
        }

        return formatMetricName;
    }

    private string FormatMetricLabel(IDictionary<string, string>? properties)
    {
        properties = properties != null && properties.Count > 0
            ? new Dictionary<string, string>(properties)
            : new Dictionary<string, string>();
        properties.Add(N9EClientConstant.LabelService, ServiceNameHelper.GetServiceName(_configuration));
        properties.Add(N9EClientConstant.LabelHostName, _hostName);
        properties.Add(N9EClientConstant.LabelTimestamp, DateTimeOffset.UtcNow.ToString(N9EClientConstant.DataTimeFormat));
        var builder = new StringBuilder("{");
        foreach (var property in properties)
        {
            builder.Append(property.Key).Append("=").Append("\"").Append(property.Value).Append("\",");
        }
        builder.Length -= 1;
        builder.Append("}");
        
        return builder.ToString();
    }

    private void TrackMetric(string metric)
    {
        try
        {
            if (_n9EClientForLoggingOptions.CurrentValue.DisableLogging)
            {
                return;
            }

            _logger.Warning(metric);
        }
        catch (Exception e)
        {
            // No need to handle monitoring log printing exceptions.
        }
    }
}