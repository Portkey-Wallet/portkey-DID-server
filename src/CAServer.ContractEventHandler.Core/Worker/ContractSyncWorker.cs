using System;
using System.Diagnostics;
using System.Threading.Tasks;
using CAServer.Common;
using CAServer.ContractEventHandler.Core.Application;
using CAServer.Nightingale;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace CAServer.ContractEventHandler.Core.Worker;

public class ContractSyncWorker : AsyncPeriodicBackgroundWorkerBase
{
    private readonly ILogger<ContractSyncWorker> _logger;
    private readonly IContractAppService _contractAppService;
    private readonly ContractSyncOptions _contractSyncOptions;
    private readonly IBackgroundWorkerRegistrarProvider _registrarProvider;
    private readonly N9EClientFactory _n9EClientFactory;

    private const string WorkerName = "ContractSyncWorker";


    public ContractSyncWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        IContractAppService contractAppService, IOptions<ContractSyncOptions> workerOptions,
        IBackgroundWorkerRegistrarProvider registrarProvider, IHostApplicationLifetime hostApplicationLifetime,
        N9EClientFactory n9EClientFactory, ILogger<ContractSyncWorker> logger) : base(
        timer,
        serviceScopeFactory)
    {
        _contractSyncOptions = workerOptions.Value;
        _contractAppService = contractAppService;
        _registrarProvider = registrarProvider;
        _n9EClientFactory = n9EClientFactory;
        _logger = logger;
        Timer.Period = 1000 * _contractSyncOptions.Sync;
        
        hostApplicationLifetime.ApplicationStopped.Register(async () =>
        {
            _logger.LogWarning("[Stopped]remove work node " + WorkerName);
            await _registrarProvider.TryRemoveWorkerNodeAsync(WorkerName);
        });

        hostApplicationLifetime.ApplicationStopping.Register(async () =>
        {
            _logger.LogWarning("[Stopping]remove work node " + WorkerName);
            await _registrarProvider.TryRemoveWorkerNodeAsync(WorkerName);
        });
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        if (!await _registrarProvider.RegisterUniqueWorkerNodeAsync(WorkerName, _contractSyncOptions.Sync,
                _contractSyncOptions.WorkerNodeExpirationTime))
        {
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        try
        {
            await _contractAppService.QueryAndSyncAsync();
        }
        finally
        {
            stopwatch.Stop();
            await _registrarProvider.TryRemoveWorkerNodeAsync(WorkerName);
            await _n9EClientFactory.TrackTransactionSync(N9EClientConstant.Biz, WorkerName,
                stopwatch.ElapsedMilliseconds);
        }
    }
}