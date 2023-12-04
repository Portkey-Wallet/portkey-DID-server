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
    private readonly IPayRedPackageTask _packageTask;
    private readonly IDistributedEventBus _distributedEventBus;

    public PayRedPackageEventHandler(IDistributedEventBus distributedEventBus,
        ILogger<RedPackageEventHandler> logger, IPayRedPackageTask packageTask)
    {
        _distributedEventBus = distributedEventBus;
        _logger = logger;
        _packageTask = packageTask;
    }

    public async Task HandleEventAsync(PayRedPackageEto eventData)
    {
      
        _logger.LogInformation($"PayRedPackageAsync start and the red package id is {eventData.RedPackageId}",eventData.RedPackageId.ToString());

        await _packageTask.PayRedPackageAsync(eventData.RedPackageId);
    }
}