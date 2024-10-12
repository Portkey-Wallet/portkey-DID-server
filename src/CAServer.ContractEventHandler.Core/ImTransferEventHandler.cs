using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using CAServer.ContractEventHandler.Core.Application;
using CAServer.ImTransfer.Etos;
using CAServer.Monitor.Interceptor;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace CAServer.ContractEventHandler.Core;

public class ImTransferEventHandler : IDistributedEventHandler<TransferEto>, ITransientDependency
{
    private readonly IImTransferService _imTransferService;

    public ImTransferEventHandler(IImTransferService imTransferService)
    {
        _imTransferService = imTransferService;
    }

    [ExceptionHandler(typeof(Exception),
        Message = "ImTransferEventHandler TransferEto exist error",  
        TargetType = typeof(ExceptionHandlingService), 
        MethodName = nameof(ExceptionHandlingService.HandleExceptionP1))
    ]
    public Task HandleEventAsync(TransferEto eventData)
    {
        _ = _imTransferService.TransferAsync(eventData);
        return Task.CompletedTask;
    }
}