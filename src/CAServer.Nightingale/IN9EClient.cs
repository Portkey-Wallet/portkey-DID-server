using Orleans.Runtime;

namespace CAServer.Nightingale;

public interface IN9EClient
{
    //IMetricTelemetryConsumer
    public void TrackMetric(string name, double value, IDictionary<string, string>? properties = null);

    public void TrackMetric(string name, TimeSpan value, IDictionary<string, string> properties = null);

    public void IncrementMetric(string name);

    public void IncrementMetric(string name, double value);

    public void DecrementMetric(string name);

    public void DecrementMetric(string name, double value);
    
    //ITraceTelemetryConsumer
    public void TrackTrace(string message);

    public void TrackTrace(string message, Severity severity);

    public void TrackTrace(string message, Severity severity, IDictionary<string, string> properties);

    public void TrackTrace(string message, IDictionary<string, string> properties);
    
    //IRequestTelemetryConsumer
    public void TrackRequest(string name, DateTimeOffset startTime, TimeSpan duration, string responseCode,
        bool success);

    //IDependencyTelemetryConsumer
    public void TrackDependency(string dependencyName, string commandName, DateTimeOffset startTime, TimeSpan duration,
        bool success);

    //IExceptionTelemetryConsumer
    public void TrackException(Exception exception, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null);

    //IEventTelemetryConsumer
    public void TrackEvent(string eventName, IDictionary<string, string> properties = null,
        IDictionary<string, double> metrics = null);

    public bool IsEnabled();

    public void Flush();

    public void Close();
}