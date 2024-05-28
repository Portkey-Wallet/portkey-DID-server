using System.Diagnostics;
using System.Threading.Tasks;
using CAServer.Common;
using CAServer.ContractEventHandler.Core.Application;
using CAServer.Nightingale;
using CAServer.UserAssets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
    private readonly NFTTraitsSyncOptions _nftTraitsSyncOptions;
    private readonly ILogger<NftTraitsProportionCalculateWorker> _logger;

    private const string WorkerName = "NftTraitsProportionCalculateWorker";

    public NftTraitsProportionCalculateWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        IUserAssetsAppService userAssetsAppService,
        IBackgroundWorkerRegistrarProvider registrarProvider, N9EClientFactory n9EClientFactory,
        IOptionsSnapshot<NFTTraitsSyncOptions> nftTraitsSyncOptions,
        IOptionsSnapshot<ContractSyncOptions> contractSyncOptions, ILogger<NftTraitsProportionCalculateWorker> logger) : base(timer,
        serviceScopeFactory)
    {
        _userAssetsAppService = userAssetsAppService;
        _registrarProvider = registrarProvider;
        _n9EClientFactory = n9EClientFactory;
        _logger = logger;
        _contractSyncOptions = contractSyncOptions.Value;
        _nftTraitsSyncOptions = nftTraitsSyncOptions.Value;
        Timer.Period = 1000 * _nftTraitsSyncOptions.Sync;
    }


    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        _logger.LogInformation("Start Sync NFT traits begin");
        if (!await _registrarProvider.RegisterUniqueWorkerNodeAsync(WorkerName, _nftTraitsSyncOptions.Sync,
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