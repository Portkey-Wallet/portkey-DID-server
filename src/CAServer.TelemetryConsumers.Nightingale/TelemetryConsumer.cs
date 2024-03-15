using System.Collections.Concurrent;
using System.Timers;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Orleans.Runtime;
using Serilog;
using Serilog.Events;

namespace Orleans.TelemetryConsumers.Nightingale
{
    public class TelemetryConsumer : IEventTelemetryConsumer, IExceptionTelemetryConsumer, IDependencyTelemetryConsumer,
        IMetricTelemetryConsumer, IRequestTelemetryConsumer
    {
        private readonly ILogger _logger;


        private static readonly ConcurrentDictionary<string, string> Metric = new ConcurrentDictionary<string, string>();
        
        public TelemetryConsumer(IServiceProvider serviceProvider)
        {
            _logger = LogHelper.CreateLogger(LogEventLevel.Debug);
            
            NightingaleClient.Logger = _logger;
            //NightingaleClient.StartAgent();

            
            var timer = new System.Timers.Timer(1000);
            timer.Elapsed += OnTimerOnElapsed;
            timer.AutoReset = true;
            timer.Enabled = true;
        }

        private void OnTimerOnElapsed(object? sender, ElapsedEventArgs e)
        {
            Random random = new Random();
            // _logger.Error("#metrics********************************** {0}", Metric.Count);
            // var list = new List<string>(Metric.Keys);
            // list.Sort();
            // _logger.Error("#" + JsonConvert.SerializeObject(list));
            // _logger.Error("#metrics**********************************");
            
            var nextDouble = random.NextDouble();
            if (nextDouble > 0.2)
            {
                _logger.Error("Grain_Call{Interface=\"IBookmarkGrain\", Method=\"AddBookMark\"} " + 50 * nextDouble);
            }
            _logger.Error("Grain_Call{Interface=\"IUpgradeGrain\", Method=\"AddUpgradeInfo\"} " + 200 * random.NextDouble());
        }

        public void DecrementMetric(string name)
        {
            Metric.TryAdd(name, string.Empty);
            NightingaleClient.RecordMetric(FormatMetricName(name), -1);
        }

        public void DecrementMetric(string name, double value)
        {
            Metric.TryAdd(name, string.Empty);
            NightingaleClient.RecordMetric(FormatMetricName(name), (float)value * -1);
        }

        public void IncrementMetric(string name)
        {
            Metric.TryAdd(name, string.Empty);
            NightingaleClient.RecordMetric(FormatMetricName(name), 1);
            NightingaleClient.IncrementCounter(name);
        }

        public void IncrementMetric(string name, double value)
        {
            Metric.TryAdd(name, string.Empty);
            NightingaleClient.RecordMetric(FormatMetricName(name), (float)value);
        }

        public void TrackDependency(string dependencyName, string commandName, DateTimeOffset startTime,
            TimeSpan duration, bool success)
        {
            Metric.TryAdd($"{dependencyName}/{commandName}", string.Empty);
            NightingaleClient.RecordResponseTimeMetric(FormatMetricName($"{dependencyName}/{commandName}"),
                (long)duration.TotalMilliseconds);
        }

        public void TrackEvent(string eventName, IDictionary<string, string> properties = null,
            IDictionary<string, double> metrics = null)
        {
            Metric.TryAdd(eventName, string.Empty);
            NightingaleClient.RecordCustomEvent(eventName,
                metrics != null ? metrics.ToDictionary(e => e.Key, e => (object)e.Value) : null);
            AddMetric(metrics);
            AddProperties(properties);
        }

        public void TrackException(Exception exception, IDictionary<string, string> properties = null,
            IDictionary<string, double> metrics = null)
        {
            AddMetric(metrics);
            NightingaleClient.NoticeError(exception, properties);
        }

        public void TrackMetric(string name, TimeSpan value, IDictionary<string, string> properties = null)
        {
            Metric.TryAdd(name, string.Empty);
            AddProperties(properties);
            NightingaleClient.RecordMetric(FormatMetricName(name), (float)value.TotalMilliseconds);
        }

        public void TrackMetric(string name, double value, IDictionary<string, string> properties = null)
        {
            Metric.TryAdd(name, string.Empty);
            AddProperties(properties);
            NightingaleClient.RecordMetric(FormatMetricName(name), (float)value);
        }

        public void TrackRequest(string name, DateTimeOffset startTime, TimeSpan duration, string responseCode,
            bool success)
        {
            Metric.TryAdd(name, string.Empty);
            NightingaleClient.RecordMetric(FormatMetricName(name), (float)duration.TotalMilliseconds);
        }

        private static string FormatMetricName(string name)
        {
            Metric.TryAdd(name, string.Empty);
            // according to NR docs https://docs.newrelic.com/docs/agents/manage-apm-agents/agent-data/custom-metrics
            // if is required to prefix all custom metrics with "Custom/"
            return name.Replace(".", "_") + "{host=\"test4-portkey-v2-service-1\", timestamp= \"" +  DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString() + "\"}";
        }

        private static void AddMetric(IDictionary<string, double> metrics)
        {
            if (metrics != null)
            {
                metrics.AsParallel().ForAll(m =>
                {
                    Metric.TryAdd(m.Key, string.Empty);
                    NightingaleClient.AddCustomParameter(m.Key, m.Value);
                });
            }
        }

        private static void AddProperties(IDictionary<string, string> properties)
        {
            if (properties != null)
            {
                properties.AsParallel().ForAll(p =>
                {
                    NightingaleClient.AddCustomParameter(p.Key, p.Value);
                });
            }
        }

        public void Flush()
        {
        }

        public void Close()
        {
        }
    }
}