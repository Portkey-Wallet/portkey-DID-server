using System.Threading.Tasks;
using CAServer.EntityEventHandler.Core.Service;
using CAServer.Guardian;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace CAServer.EntityEventHandler.Core;

public class GuardianHandler : IDistributedEventHandler<GuardianEto>, IDistributedEventHandler<GuardianDeleteEto>,
    ITransientDependency
{
    private readonly IGuardianService _guardianService;

    public GuardianHandler(IGuardianService guardianService)
    {
        _guardianService = guardianService;
    }

    public async Task HandleEventAsync(GuardianEto eventData)
    {
        _ = _guardianService.AddGuardianAsync(eventData);
    }

    public async Task HandleEventAsync(GuardianDeleteEto eventData)
    {
      _ = _guardianService.DeleteGuardianAsync(eventData);
    }
}