using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using CAServer.ContractEventHandler.Core.Application;
using CAServer.Monitor.Interceptor;
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

    [ExceptionHandler(typeof(Exception),
        Message = "RedPackageEventHandler RedPackageCreateEto exist error",  
        TargetType = typeof(ExceptionHandlingService), 
        MethodName = nameof(ExceptionHandlingService.HandleExceptionP1))
    ]
    public async Task HandleEventAsync(RedPackageCreateEto eventData)
    {
       
        _ =  _contractAppService.CreateRedPackageAsync(eventData);
        
    }
}