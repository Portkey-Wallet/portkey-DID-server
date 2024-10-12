using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using CAServer.Commons;
using CAServer.ContractEventHandler.Core.Application;
using CAServer.Etos;
using CAServer.Monitor.Interceptor;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace CAServer.ContractEventHandler.Core;

public class CAContractEventHandler : IDistributedEventHandler<AccountRegisterCreateEto>,
    IDistributedEventHandler<AccountRecoverCreateEto>, ITransientDependency
{
    private readonly IContractAppService _contractAppService;
    private readonly ILogger<CAContractEventHandler> _logger;

    public CAContractEventHandler(IContractAppService contractAppService, ILogger<CAContractEventHandler> logger)
    {
        _contractAppService = contractAppService;
        _logger = logger;
    }

    [ExceptionHandler(typeof(Exception),
        Message = "CAContractEventHandler AccountRegisterCreateEto exist error",  
        TargetType = typeof(ExceptionHandlingService), 
        MethodName = nameof(ExceptionHandlingService.HandleExceptionP1))
    ]
    public async Task HandleEventAsync(AccountRegisterCreateEto eventData)
    {
        // CreateHolderInfo can take a long time
        _ = _contractAppService.CreateHolderInfoAsync(eventData);
    }

    [ExceptionHandler(typeof(Exception),
        Message = "CAContractEventHandler AccountRecoverCreateEto exist error",  
        TargetType = typeof(ExceptionHandlingService), 
        MethodName = nameof(ExceptionHandlingService.HandleExceptionP1))
    ]
    public async Task HandleEventAsync(AccountRecoverCreateEto eventData)
    {
        // SocialRecovery can take a long time
        _ = _contractAppService.SocialRecoveryAsync(eventData);
    }
}