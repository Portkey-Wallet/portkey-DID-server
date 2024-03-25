using System.Threading.Tasks;
using CAServer.Common;
using CAServer.Hub;
using CAServer.Hubs;
using CAServer.Tab.Dtos;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.Auditing;

namespace CAServer.Tab;

[RemoteService(false), DisableAuditing]
public class TabAppService : CAServerAppService, ITabAppService
{
    private readonly IHubProvider _caHubProvider;
    private readonly IConnectionProvider _connectionProvider;
    private readonly IHttpClientService _httpClientService;

    public TabAppService(IHubProvider caHubProvider, IConnectionProvider connectionProvider,
        IHttpClientService httpClientService)
    {
        _caHubProvider = caHubProvider;
        _connectionProvider = connectionProvider;
        _httpClientService = httpClientService;
    }

    public async Task CompleteAsync(TabCompleteDto input)
    {
        var connectionInfo = _connectionProvider.GetConnectionByClientId(input.ClientId);
        if (connectionInfo == null)
        {
            // var routeInfo = await _routeTableProvider.GetRouteTableInfoAsync(input.ClientId);
            // var url = $"http://{routeInfo.ConnectionIp}:{routeInfo.Port}/app/api/tab/complete";
            //
            // await _httpClientService.PostAsync<object>(url, input);
            // Logger.LogInformation("send to service, url:{url}, clientId:{clientId}, methodName:{methodName}",
            //     url, input.ClientId, input.MethodName);
            return;
        }

        await SendAsync(input);
    }

    public async Task ReceiveAsync(TabCompleteDto input)
    {
        var connectionInfo = _connectionProvider.GetConnectionByClientId(input.ClientId);
        if (connectionInfo == null)
        {
            Logger.LogError("send to service, clientId:{clientId}, methodName:{methodName}",input.ClientId, input.MethodName);
            return;
        }

        await SendAsync(input);
    }

    private async Task SendAsync(TabCompleteDto input)
    {
        await _caHubProvider.ResponseAsync(
            new Hubs.HubResponse<string> { Body = input.Data, RequestId = input.ClientId },
            input.ClientId, input.MethodName);

        Logger.LogInformation("send hub success, clientId:{clientId}, methodName:{methodName}",
            input.ClientId, input.MethodName);
    }
}