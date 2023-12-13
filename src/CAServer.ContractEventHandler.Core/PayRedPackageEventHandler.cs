using System.Threading.Tasks;
using CAServer.ContractEventHandler.Core.Application;
using CAServer.RedPackage.Etos;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace CAServer.ContractEventHandler.Core;

public class PayRedPackageEventHandler : IDistributedEventHandler<PayRedPackageEto>, ITransientDependency
{
    private readonly ILogger<RedPackageEventHandler> _logger;
    private readonly IContractAppService _contractAppService;


    public PayRedPackageEventHandler(ILogger<RedPackageEventHandler> logger,  IContractAppService contractAppService)
    {
        _logger = logger;
        _contractAppService = contractAppService;
    }

    public async Task HandleEventAsync(PayRedPackageEto eventData)
    {
        _ = _contractAppService.PayRedPackageAsync(eventData.RedPackageId);
    }
}