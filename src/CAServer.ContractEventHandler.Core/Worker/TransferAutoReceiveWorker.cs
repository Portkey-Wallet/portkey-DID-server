using System.Diagnostics;
using System.Threading.Tasks;
using CAServer.Common;
using CAServer.ContractEventHandler.Core.Application;
using CAServer.Nightingale;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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

    private const string WorkerName = "TransferAutoReceiveWorker";

    public TransferAutoReceiveWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        ICrossChainTransferAppService crossChainTransferAppService, IOptions<ContractSyncOptions> workerOptions,
        IBackgroundWorkerRegistrarProvider registrarProvider, IHostApplicationLifetime hostApplicationLifetime,
        N9EClientFactory n9EClientFactory) : base(
        timer,
        serviceScopeFactory)
    {
        _contractSyncOptions = workerOptions.Value;
        _crossChainTransferAppService = crossChainTransferAppService;
        _registrarProvider = registrarProvider;
        _n9EClientFactory = n9EClientFactory;

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
            await _crossChainTransferAppService.AutoReceiveAsync();
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