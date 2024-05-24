using System;
using System.Diagnostics;
using System.Threading.Tasks;
using CAServer.Common;
using CAServer.ContractEventHandler.Core.Application;
using CAServer.Nightingale;
using CAServer.UserAssets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace CAServer.ContractEventHandler.Core.Worker;

public class NftTraitsProportionCalculateWorker : AsyncPeriodicBackgroundWorkerBase
{
    private readonly IUserAssetsAppService _userAssetsAppService;
    private readonly ContractSyncOptions _contractSyncOptions;
    private readonly IBackgroundWorkerRegistrarProvider _registrarProvider;
    private readonly N9EClientFactory _n9EClientFactory;

    private const string WorkerName = "NftTraitsProportionCalculateWorker";

    public NftTraitsProportionCalculateWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        IUserAssetsAppService userAssetsAppService, ContractSyncOptions contractSyncOptions,
        IBackgroundWorkerRegistrarProvider registrarProvider, N9EClientFactory n9EClientFactory) : base(timer,
        serviceScopeFactory)
    {
        _userAssetsAppService = userAssetsAppService;
        _contractSyncOptions = contractSyncOptions;
        _registrarProvider = registrarProvider;
        _n9EClientFactory = n9EClientFactory;
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
            await _userAssetsAppService.NftTraitsProportionCalculateAsync();
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