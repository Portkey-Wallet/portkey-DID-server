using System.Threading.Tasks;
using CAServer.ContractEventHandler.Core.Application;
using CAServer.RedPackage.Etos;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace CAServer.ContractEventHandler.Core;

public class RedPackageEventHandler : IDistributedEventHandler<RedPackageCreateEto>, ITransientDependency
{
    private readonly IContractAppService _contractAppService;


    public RedPackageEventHandler(IContractAppService contractAppService)
    {
        _contractAppService = contractAppService;
    }

    public async Task HandleEventAsync(RedPackageCreateEto eventData)
    {
       
        _ =  _contractAppService.CreateRedPackageAsync(eventData);
        
    }
}