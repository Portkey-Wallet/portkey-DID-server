using System.Threading.Tasks;
using CAServer.EntityEventHandler.Core.Service;
using CAServer.Etos;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace CAServer.EntityEventHandler.Core;

public class HolderStatisticHandler : IDistributedEventHandler<HolderExtraInfoEto>,
    IDistributedEventHandler<HolderExtraInfoCompletedEto>, ITransientDependency
{
    private readonly IHolderStatisticService _holderStatisticService;

    public HolderStatisticHandler(IHolderStatisticService holderStatisticService)
    {
        _holderStatisticService = holderStatisticService;
    }

    public async Task HandleEventAsync(HolderExtraInfoEto eventData)
    {
        _ = _holderStatisticService.HandleHolderExtraInfoAsync(eventData);
    }

    public async Task HandleEventAsync(HolderExtraInfoCompletedEto eventData)
    {
        _ = _holderStatisticService.HandleHolderExtraInfoCompletedAsync(eventData);
    }
}