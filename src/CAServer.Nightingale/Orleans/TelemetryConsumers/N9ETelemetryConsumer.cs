using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Runtime;

namespace CAServer.Nightingale.Orleans.TelemetryConsumers
{
    public class N9ETelemetryConsumer : IEventTelemetryConsumer, IExceptionTelemetryConsumer,
        IDependencyTelemetryConsumer, IMetricTelemetryConsumer, IRequestTelemetryConsumer, ITraceTelemetryConsumer
    {
        private readonly ILogger? _logger;
        private readonly N9EClientFactory? _n9EClientFactory;

        public N9ETelemetryConsumer(IServiceProvider serviceProvider, IOptions<N9EOptions> options,
            ILoggerFactory loggerFactory)
        {
            _logger = serviceProvider.GetService<ILogger<N9ETelemetryConsumer>>();
            _n9EClientFactory = serviceProvider.GetService<N9EClientFactory>();
        }

        public void DecrementMetric(string name)
        {
            Track(client => client.DecrementMetric(name));
        }

        public void DecrementMetric(string name, double value)
        {
            Track(client => client.DecrementMetric(name, value));
        }

        public void IncrementMetric(string name)
        {
            Track(client => client.IncrementMetric(name));
        }

        public void IncrementMetric(string name, double value)
        {
            Track(client => client.IncrementMetric(name, value));
        }

        public void TrackDependency(string dependencyName, string commandName, DateTimeOffset startTime,
            TimeSpan duration, bool success)
        {
            Track(client => client.TrackDependency(dependencyName, commandName, startTime, duration, success));
        }

        public void TrackEvent(string eventName, IDictionary<string, string> properties = null,
            IDictionary<string, double> metrics = null)
        {
            Track(client => client.TrackEvent(eventName, properties, metrics));
        }

        public void TrackException(Exception exception, IDictionary<string, string> properties = null,
            IDictionary<string, double> metrics = null)
        {
            Track(client => client.TrackException(exception, properties, metrics));
        }

        public void TrackMetric(string name, TimeSpan value, IDictionary<string, string> properties = null)
        {
            Track(client => client.TrackMetric(name, value, properties));
        }

        public void TrackMetric(string name, double value, IDictionary<string, string>? properties = null)
        {
            Track(client => client.TrackMetric(name, value, properties));
        }

        public void TrackRequest(string name, DateTimeOffset startTime, TimeSpan duration, string responseCode,
            bool success)
        {
            Track(client => client.TrackRequest(name, startTime, duration, responseCode, success));
        }

        public void TrackTrace(string message)
        {
            Track(client => client.TrackTrace(message));
        }

        public void TrackTrace(string message, Severity severity)
        {
            Track(client => client.TrackTrace(message, severity));
        }

        public void TrackTrace(string message, Severity severity, IDictionary<string, string> properties)
        {
            Track(client => client.TrackTrace(message, severity, properties));
        }

        public void TrackTrace(string message, IDictionary<string, string> properties)
        {
            Track(client => client.TrackTrace(message, properties));
        }

        public void Flush()
        {
            Track(client => client.Flush());
        }

        public void Close()
        {
            Track(client => client.Close());
        }

        private void Track(Action<IN9EClient> trackDelegate)
        {
            var n9EClients = _n9EClientFactory?.N9EClients;
            if (n9EClients == null)
            {
                return;
            }

            foreach (var n9EClient in n9EClients)
            {
                if (!n9EClient.IsEnabled())
                {
                    continue;
                }

                trackDelegate(n9EClient);
            }
        }
    }
}