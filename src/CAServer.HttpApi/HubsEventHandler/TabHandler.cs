using System.Threading.Tasks;
using CAServer.Hubs;
using CAServer.Tab.Etos;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace CAServer.HubsEventHandler;

public class TabHandler : IDistributedEventHandler<TabCompleteEto>, ITransientDependency
{
    private readonly IHubProvider _caHubProvider;
    private readonly ILogger<TabHandler> _logger;

    public TabHandler(IHubProvider caHubProvider, ILogger<TabHandler> logger)
    {
        _caHubProvider = caHubProvider;
        _logger = logger;
    }

    public async Task HandleEventAsync(TabCompleteEto eventData)
    {
        await _caHubProvider.ResponseAsync(
            new HubResponse<string> { Body = eventData.Data, RequestId = eventData.ClientId },
            eventData.ClientId, eventData.MethodName);

        _logger.LogInformation("tab communication success, clientId:{clientId}, methodName:{methodName}",
            eventData.ClientId, eventData.MethodName);
    }
}