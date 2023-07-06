using System.Threading.Tasks;
using CAServer.Hubs;
using CAServer.Message.Etos;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace CAServer.EntityEventHandler.Core;

public class ScanLoginHandler : IDistributedEventHandler<ScanLoginEto>, ITransientDependency
{
    private readonly IHubProvider _caHubProvider;

    public ScanLoginHandler(IHubProvider caHubProvider)
    {
        _caHubProvider = caHubProvider;
    }

    public async Task HandleEventAsync(ScanLoginEto eventData)
    {
        await _caHubProvider.ResponseAsync(
            new HubResponse<string> { Body = eventData.Message, RequestId = eventData.TargetClientId },
            eventData.TargetClientId, eventData.MethodName, true);
    }
}