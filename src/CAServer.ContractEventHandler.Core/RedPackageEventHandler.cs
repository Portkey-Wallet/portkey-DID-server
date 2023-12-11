using System;
using System.Linq;
using System.Threading.Tasks;
using CAServer.ContractEventHandler.Core.Application;
using CAServer.Grains.Grain.ApplicationHandler;
using CAServer.RedPackage.Etos;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;


namespace CAServer.ContractEventHandler.Core;

public class RedPackageEventHandler : IDistributedEventHandler<RedPackageCreateEto>, ITransientDependency
{
    private readonly ILogger<RedPackageEventHandler> _logger;
    private readonly IContractAppService _contractAppService;
    private readonly IDistributedEventBus _distributedEventBus;


    public RedPackageEventHandler(IContractAppService contractAppService, IDistributedEventBus distributedEventBus,
        ILogger<RedPackageEventHandler> logger)
    {
        _contractAppService = contractAppService;
        _distributedEventBus = distributedEventBus;
        _logger = logger;
    }

    public async Task HandleEventAsync(RedPackageCreateEto eventData)
    {
       
        _ =  _contractAppService.CreateRedPackageAsync(eventData);
        
    }
}