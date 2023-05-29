using System.Threading.Tasks;
using CAServer.ContractEventHandler.Core.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace CAServer.ContractEventHandler.Core.Worker;

public class TransferAutoReceiveWorker : AsyncPeriodicBackgroundWorkerBase
{
    private readonly ICrossChainTransferAppService _crossChainTransferAppService;
    private readonly ContractSyncOptions _contractSyncOptions;

    public TransferAutoReceiveWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        ICrossChainTransferAppService crossChainTransferAppService, IOptions<ContractSyncOptions> workerOptions) : base(
        timer,
        serviceScopeFactory)
    {
        _contractSyncOptions = workerOptions.Value;
        _crossChainTransferAppService = crossChainTransferAppService;

        Timer.Period = 1000 * _contractSyncOptions.AutoReceive;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        await _crossChainTransferAppService.AutoReceiveAsync();
    }
}