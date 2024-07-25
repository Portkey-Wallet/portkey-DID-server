using System.Threading.Tasks;
using CAServer.ContractEventHandler.Core.Application;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace CAServer.ContractEventHandler.Core.Worker;

public class ChainHeightWorker : AsyncPeriodicBackgroundWorkerBase
{
    private readonly IChainHeightService _chainHeightService;
    public ChainHeightWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory, IChainHeightService chainHeightService) : base(timer,
        serviceScopeFactory)
    {
        _chainHeightService = chainHeightService;
        Timer.Period = 1000 * 5;
        Timer.RunOnStart = true;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        await _chainHeightService.SetChainHeightAsync();
    }
}