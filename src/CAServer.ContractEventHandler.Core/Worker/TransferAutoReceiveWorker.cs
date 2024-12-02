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

public class TransferAutoReceiveWorker : AsyncPeriodicBackgroundWorkerBase
{
    private readonly ICrossChainTransferAppService _crossChainTransferAppService;
    private readonly ContractSyncOptions _contractSyncOptions;
    private readonly IBackgroundWorkerRegistrarProvider _registrarProvider;
    private readonly N9EClientFactory _n9EClientFactory;
    private readonly ILogger<TransferAutoReceiveWorker> _logger;

    private const string WorkerName = "TransferAutoReceiveWorker";

    public TransferAutoReceiveWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        ICrossChainTransferAppService crossChainTransferAppService, IOptions<ContractSyncOptions> workerOptions,
        IBackgroundWorkerRegistrarProvider registrarProvider, IHostApplicationLifetime hostApplicationLifetime,
        N9EClientFactory n9EClientFactory, ILogger<TransferAutoReceiveWorker> logger) : base(
        timer,
        serviceScopeFactory)
    {
        _contractSyncOptions = workerOptions.Value;
        _crossChainTransferAppService = crossChainTransferAppService;
        _registrarProvider = registrarProvider;
        _n9EClientFactory = n9EClientFactory;
        _logger = logger;

        Timer.Period = 1000 * _contractSyncOptions.AutoReceive;

        hostApplicationLifetime.ApplicationStopped.Register(() =>
        {
            _registrarProvider.TryRemoveWorkerNodeAsync(WorkerName);
        });
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        if (!await _registrarProvider.RegisterUniqueWorkerNodeAsync(WorkerName, _contractSyncOptions.AutoReceive,
                _contractSyncOptions.WorkerNodeExpirationTime))
        {
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        try
        {
            _logger.LogInformation("[AutoReceive] TransferAutoReceiveWorker start.");
            await _crossChainTransferAppService.AutoReceiveAsync();
            _logger.LogInformation("[AutoReceive] TransferAutoReceiveWorker end.");
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