using System.Threading.Tasks;
using CAServer.ContractEventHandler.Core.Application;
using CAServer.Etos;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace CAServer.ContractEventHandler.Core;

public class ZkSinglePoseidonHashEventHandler : IDistributedEventHandler<ZkSinglePoseidonHashEto>, ITransientDependency
{
    private readonly IContractAppService _contractAppService;
    private readonly ILogger<ZkSinglePoseidonHashEventHandler> _logger;

    public ZkSinglePoseidonHashEventHandler(IContractAppService contractAppService,
        ILogger<ZkSinglePoseidonHashEventHandler> logger)
    {
        _contractAppService = contractAppService;
        _logger = logger;
    }
    
    public Task HandleEventAsync(ZkSinglePoseidonHashEto eventData)
    {
        throw new System.NotImplementedException();
    }
}