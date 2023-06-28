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
        await _caHubProvider.ResponseAsync(new HubResponseBase<string> { Body = eventData.Message },
            eventData.TargetClientId, eventData.MethodName);
    }
}