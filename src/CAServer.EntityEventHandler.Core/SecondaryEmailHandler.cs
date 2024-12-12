using System.Threading.Tasks;
using CAServer.EntityEventHandler.Core.Service;
using CAServer.Etos;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace CAServer.EntityEventHandler.Core;

public class SecondaryEmailHandler : IDistributedEventHandler<AccountEmailEto>, ITransientDependency
{
    private readonly ISecondaryEmailService _secondaryEmailService;

    public SecondaryEmailHandler(ISecondaryEmailService secondaryEmailService)
    {
        _secondaryEmailService = secondaryEmailService;
    }

    public async Task HandleEventAsync(AccountEmailEto eventData)
    {
        _ = _secondaryEmailService.HandleAccountEmailAsync(eventData);
    }
}