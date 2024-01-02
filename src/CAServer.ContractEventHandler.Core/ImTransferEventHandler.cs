using System.Threading.Tasks;
using CAServer.ContractEventHandler.Core.Application;
using CAServer.ImTransfer.Etos;
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

    public Task HandleEventAsync(TransferEto eventData)
    {
        _ = _imTransferService.TransferAsync(eventData);
        return Task.CompletedTask;
    }
}