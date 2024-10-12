using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using CAServer.ContractEventHandler.Core.Application;
using CAServer.Monitor.Interceptor;
using CAServer.RedPackage.Etos;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace CAServer.ContractEventHandler.Core;

public class RefundRedPackageEventHandler : IDistributedEventHandler<RefundRedPackageEto>, ITransientDependency
{
    private readonly ILogger<RedPackageEventHandler> _logger;
    private readonly IContractAppService _contractAppService;


    public RefundRedPackageEventHandler(ILogger<RedPackageEventHandler> logger,  IContractAppService contractAppService)
    {
        _logger = logger;
        _contractAppService = contractAppService;
    }

    [ExceptionHandler(typeof(Exception),
        Message = "RefundRedPackageEventHandler RefundRedPackageEto exist error",  
        TargetType = typeof(ExceptionHandlingService), 
        MethodName = nameof(ExceptionHandlingService.HandleExceptionP1))
    ]
    public async Task HandleEventAsync(RefundRedPackageEto eventData)
    {
        _ = _contractAppService.RefundAsync(eventData.RedPackageId);
    }
}