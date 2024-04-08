using System;
using System.Threading.Tasks;
using CAServer.Common;
using CAServer.ContractEventHandler.Core.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace CAServer.ContractEventHandler.Core.Worker;

public class ContractSyncWorker : AsyncPeriodicBackgroundWorkerBase
{
    private readonly IContractAppService _contractAppService;
    private readonly ContractSyncOptions _contractSyncOptions;
    private readonly IBackgroundWorkerRegistrarProvider _registrarProvider;

    private const string WorkerName = "ContractSyncWorker";


    public ContractSyncWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        IContractAppService contractAppService, IOptions<ContractSyncOptions> workerOptions,
        IBackgroundWorkerRegistrarProvider registrarProvider, IHostApplicationLifetime hostApplicationLifetime) : base(
        timer,
        serviceScopeFactory)
    {
        _contractSyncOptions = workerOptions.Value;
        _contractAppService = contractAppService;
        _registrarProvider = registrarProvider;
        Timer.Period = 1000 * _contractSyncOptions.Sync;

        hostApplicationLifetime.ApplicationStopped.Register(() =>
        {
            _registrarProvider.TryRemoveWorkerNodeAsync(WorkerName);
        });
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        if (!await _registrarProvider.RegisterUniqueWorkerNodeAsync(WorkerName, _contractSyncOptions.Sync))
        {
            return;
        }
        await _contractAppService.QueryAndSyncAsync();
    }
}