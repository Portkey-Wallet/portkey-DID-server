using System.Threading.Tasks;
using CAServer.Hub;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace CAServer.Hubs;

public class HubProvider : IHubProvider, ISingletonDependency
{
    private readonly IConnectionProvider _connectionProvider;
    private readonly IHubContext<CAHub> _hubContext;
    private readonly IHubCacheProvider _hubCacheProvider;
    private readonly ILogger<HubProvider> _logger;

    public HubProvider(IConnectionProvider connectionProvider, IHubContext<CAHub> hubContext, ILogger<HubProvider> logger, IHubCacheProvider hubCacheProvider)
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
            _hubCacheProvider.SetResponseAsync(new HubResponseCacheEntity<T>(res.Body, res.RequestId, method), clientId);
        }

        var connection = _connectionProvider.GetConnectionByClientId(clientId);
        if (connection == null)
        {
            _logger.LogError("connection not found by clientId={clientId}", clientId);
            return;
        }

        _logger.LogInformation("provider sync requestId={res.RequestId} to clientId={clientId} method={method} body={body}", res.RequestId, clientId, method, res.Body);
        await _hubContext.Clients.Clients(connection.ConnectionId).SendAsync(method, res);
    }
}