using System.Diagnostics;
using System.Threading.Tasks;
using CAServer.ContractEventHandler.Core.Application;
using CAServer.Nightingale;
using CAServer.ScheduledTask;
using CAServer.UserAssets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CAServer.ContractEventHandler.Core.Worker;

public class NftTraitsProportionCalculateWorker : ScheduledTaskBase
{
    private readonly IUserAssetsAppService _userAssetsAppService;
    private readonly N9EClientFactory _n9EClientFactory;
    private readonly ILogger<NftTraitsProportionCalculateWorker> _logger;

    private const string WorkerName = "NftTraitsProportionCalculateWorker";

    public NftTraitsProportionCalculateWorker(
        IUserAssetsAppService userAssetsAppService,
        N9EClientFactory n9EClientFactory,
        IOptionsSnapshot<NFTTraitsSyncOptions> nftTraitsSyncOptions,
        ILogger<NftTraitsProportionCalculateWorker> logger)
    {
        _userAssetsAppService = userAssetsAppService;
        _n9EClientFactory = n9EClientFactory;
        _logger = logger;
        Period = nftTraitsSyncOptions.Value.Sync;
    }

    protected override async Task DoWorkAsync()
    {
        _logger.LogInformation("Start Sync NFT traits begin");
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await _userAssetsAppService.NftTraitsProportionCalculateAsync();
        }
        finally
        {
            stopwatch.Stop();
            await _n9EClientFactory.TrackTransactionSync(N9EClientConstant.Biz, WorkerName,
                stopwatch.ElapsedMilliseconds);
        }
    }
}