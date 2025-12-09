using System;
using System.Threading.Tasks;
using CAServer.Hub;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Volo.Abp.DependencyInjection;

namespace CAServer.Hubs;

public class HubProvider : IHubProvider, ISingletonDependency
{
    private readonly IConnectionProvider _connectionProvider;
    private readonly IHubContext<CAHub> _hubContext;
    private readonly IHubCacheProvider _hubCacheProvider;
    private readonly ILogger<HubProvider> _logger;

    public HubProvider(IConnectionProvider connectionProvider, IHubContext<CAHub> hubContext,
        ILogger<HubProvider> logger, IHubCacheProvider hubCacheProvider)
    {
        _connectionProvider = connectionProvider;
        _hubContext = hubContext;
        _logger = logger;
        _hubCacheProvider = hubCacheProvider;
    }

    public async Task ResponseAsync<T>(HubResponse<T> res, string clientId, string method, bool isFirstTime = true)
    {
        if (isFirstTime)
        {
            _hubCacheProvider.SetResponseAsync(
                new HubResponseCacheEntity<T>(res.Body, res.RequestId, method, typeof(T)), clientId);
        }

        var connection = _connectionProvider.GetConnectionByClientId(clientId);
        if (connection == null)
        {
            _logger.LogError("connection not found by clientId={clientId}", clientId);
            return;
        }

        _logger.LogInformation(
            "provider sync requestId={requestId} to clientId={clientId} method={method} body={body}", res.RequestId,
            clientId, method, JsonConvert.SerializeObject(res.Body));

        await _hubContext.Clients.Clients(connection.ConnectionId).SendAsync(method, res);
    }

    public async Task ResponseAsync(HubResponse<object> res, string clientId, string method, Type type)
    {
        var connection = _connectionProvider.GetConnectionByClientId(clientId);
        if (connection == null)
        {
            _logger.LogError("connection not found by clientId={clientId}", clientId);
            return;
        }

        if (res.Body is JObject jObject)
        {
            res.Body = jObject.ToObject(type);
        }

        _logger.LogInformation(
            "provider sync requestId={requestId} to clientId={clientId} method={method} body={body}", res.RequestId,
            clientId, method, JsonConvert.SerializeObject(res.Body));

        await _hubContext.Clients.Clients(connection.ConnectionId).SendAsync(method, res);
    }

    public async Task ResponseAsync<T>(HubResponseBase<T> res, string clientId, string method)
    {
        var connection = _connectionProvider.GetConnectionByClientId(clientId);
        if (connection == null)
        {
            _logger.LogError("connection not found by clientId={clientId}", clientId);
            return;
        }

        _logger.LogInformation(
            "provider sync to clientId={clientId} method={method} body={body}", clientId, method,
            JsonConvert.SerializeObject(res.Body));

        await _hubContext.Clients.Clients(connection.ConnectionId).SendAsync(method, res);
    }
}