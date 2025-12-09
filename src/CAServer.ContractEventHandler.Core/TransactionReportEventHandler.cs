using System.Threading.Tasks;
using CAServer.ContractEventHandler.Core.Application;
using CAServer.DataReporting.Etos;
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

    public Task HandleEventAsync(TransactionReportEto eventData)
    {
        _ = _transactionReportAppService.HandleTransactionAsync(eventData);
        return Task.CompletedTask;
    }
}