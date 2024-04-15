using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CAServer.Nightingale;

public class N9EClientFactory
{
    private readonly ILogger<N9EClientFactory>? _logger;
    private readonly N9EOptions _n9EOptions;
    public IReadOnlyList<IN9EClient?> N9EClients { get; }

    private static readonly ConcurrentDictionary<Type, IN9EClient?> Clients =
        new ConcurrentDictionary<Type, IN9EClient?>();

    private static readonly SemaphoreSlim Semaphore = new SemaphoreSlim(1);

    public N9EClientFactory(IServiceProvider serviceProvider, IOptions<N9EOptions> options)
    {
        _logger = serviceProvider.GetService<ILogger<N9EClientFactory>>();
        _n9EOptions = options.Value;

        var n9EClients = new List<IN9EClient?>(options.Value.Clients.Count);
        foreach (var type in options.Value.Clients)
        {
            var n9EClient = CreateClient(serviceProvider, type);
            n9EClients.Add(n9EClient);
        }

        N9EClients = n9EClients;
    }

    public async Task TrackTransactionSync(string chart, string type, double duration, bool isSuccess = true,
        IDictionary<string, string>? properties = null)
    {
        properties = properties != null ? new Dictionary<string, string>(properties) : new Dictionary<string, string>();
        properties.Add(N9EClientConstant.LabelChart, chart ?? N9EClientConstant.Unknown);
        properties.Add(N9EClientConstant.LabelType, type ?? N9EClientConstant.Unknown);
        properties.Add(N9EClientConstant.LabelSuccess, isSuccess.ToString());

        await TrackAsync(client =>
        {
            client.TrackMetric(N9EClientConstant.MetricCaServerTransaction, duration, properties);
        });
    }

    public async Task TrackEventAsync(string chart, string type, int count, IDictionary<string, string>? properties = null)
    {
        properties = properties != null ? new Dictionary<string, string>(properties) : new Dictionary<string, string>();
        properties.Add(N9EClientConstant.LabelChart, chart ?? N9EClientConstant.Unknown);
        properties.Add(N9EClientConstant.LabelType, type ?? N9EClientConstant.Unknown);

        await TrackAsync(client => { client.TrackMetric(N9EClientConstant.MetricCaServerEvent, count, properties); });
    }


    internal Task TrackAsync(Action<IN9EClient> trackDelegate)
    {
        foreach (var n9EClient in N9EClients)
        {
            if (n9EClient == null || !n9EClient.IsEnabled())
            {
                continue;
            }

            trackDelegate(n9EClient);
        }

        return Task.CompletedTask;
    }

    private static IN9EClient CreateClient(IServiceProvider serviceProvider, Type clientType)
    {
        if (Clients.TryGetValue(clientType, out var n9EClient))
        {
            return n9EClient;
        }

        try
        {
            Semaphore.Wait();
            if (!Clients.TryGetValue(clientType, out n9EClient))
            {
                n9EClient = GetN9EClient(serviceProvider, clientType);
                Clients.TryAdd(clientType, n9EClient);
            }
        }
        finally
        {
            Semaphore.Release();
        }

        return n9EClient;
    }

    private static IN9EClient GetN9EClient(IServiceProvider serviceProvider, Type clientType)
    {
#pragma warning disable CS8600
        return (IN9EClient)serviceProvider.GetService(clientType) ??
#pragma warning restore CS8600
               (IN9EClient)ActivatorUtilities.CreateInstance(serviceProvider, clientType);
    }
}