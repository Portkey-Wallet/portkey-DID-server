using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using CAServer.ContractEventHandler.Core.Application;
using CAServer.DataReporting.Etos;
using CAServer.Monitor.Interceptor;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace CAServer.ContractEventHandler.Core;

public class TransactionReportEventHandler : IDistributedEventHandler<TransactionReportEto>, ITransientDependency
{
    private readonly ITransactionReportAppService _transactionReportAppService;

    public TransactionReportEventHandler(ITransactionReportAppService transactionReportAppService)
    {
        _transactionReportAppService = transactionReportAppService;
    }

    [ExceptionHandler(typeof(Exception),
        Message = "TransactionReportEventHandler TransactionReportEto exist error",  
        TargetType = typeof(ExceptionHandlingService), 
        MethodName = nameof(ExceptionHandlingService.HandleExceptionP1))
    ]
    public Task HandleEventAsync(TransactionReportEto eventData)
    {
        _ = _transactionReportAppService.HandleTransactionAsync(eventData);
        return Task.CompletedTask;
    }
}