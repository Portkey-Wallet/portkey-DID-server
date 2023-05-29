using System.Threading.Tasks;
using CAServer.ContractEventHandler.Core.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace CAServer.ContractEventHandler.Core.Worker;

public class ContractSyncWorker : AsyncPeriodicBackgroundWorkerBase
{
    private readonly IContractAppService _contractAppService;
    private readonly ContractSyncOptions _contractSyncOptions;

    public ContractSyncWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        IContractAppService contractAppService, IOptions<ContractSyncOptions> workerOptions) : base(timer,
        serviceScopeFactory)
    {
        _contractSyncOptions = workerOptions.Value;
        _contractAppService = contractAppService;

        Timer.Period = 1000 * _contractSyncOptions.Sync;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        await _contractAppService.QueryAndSyncAsync();
    }
}