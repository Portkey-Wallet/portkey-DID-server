using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using CAServer.ContractEventHandler.Core.Application;
using CAServer.Monitor.Interceptor;
using CAServer.RedPackage.Etos;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace CAServer.ContractEventHandler.Core;

public class PayRedPackageEventHandler : IDistributedEventHandler<PayRedPackageEto>, ITransientDependency
{
    private readonly IPayRedPackageService _payRedPackageService;
    private readonly ILogger<PayRedPackageEventHandler> _logger;

    public PayRedPackageEventHandler(IPayRedPackageService payRedPackageService,
        ILogger<PayRedPackageEventHandler> logger)
    {
        _payRedPackageService = payRedPackageService;
        _logger = logger;
    }

    [ExceptionHandler(typeof(Exception),
        Message = "PayRedPackageEventHandler PayRedPackageEto exist error",  
        TargetType = typeof(ExceptionHandlingService), 
        MethodName = nameof(ExceptionHandlingService.HandleExceptionP1))
    ]
    public Task HandleEventAsync(PayRedPackageEto eventData)
    {
        _logger.LogInformation("receive PayRedPackageEto RedPackageId:{0} receiverId:{1}", eventData.RedPackageId, eventData.ReceiverId);
        _ = _payRedPackageService.PayRedPackageAsync(eventData.RedPackageId);
        return Task.CompletedTask;
    }
}