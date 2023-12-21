using System.Threading.Tasks;
using CAServer.ContractEventHandler.Core.Application;
using CAServer.RedPackage.Etos;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace CAServer.ContractEventHandler.Core;

public class PayRedPackageEventHandler : IDistributedEventHandler<PayRedPackageEto>, ITransientDependency
{
    private readonly IPayRedPackageService _payRedPackageService;

    public PayRedPackageEventHandler(IPayRedPackageService payRedPackageService)
    {
        _payRedPackageService = payRedPackageService;
    }

    public Task HandleEventAsync(PayRedPackageEto eventData)
    {
        _ = _payRedPackageService.PayRedPackageAsync(eventData.RedPackageId);
        return Task.CompletedTask;
    }
}