using System.Threading.Tasks;
using CAServer.Growth;
using CAServer.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace CAServer.EntityEventHandler.Core.Worker;

public class ReferralRankWorker : AsyncPeriodicBackgroundWorkerBase
{
    private readonly IGrowthStatisticAppService _growthStatisticAppService;
    private readonly ILogger<ReferralRankWorker> _logger;
    private readonly ReferralRefreshTimeOptions _referralRefreshTimeOptions;


    public ReferralRankWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        IGrowthStatisticAppService growthStatisticAppService, ILogger<ReferralRankWorker> logger,
        IOptionsSnapshot<ReferralRefreshTimeOptions> referralRefreshTimeOptions) : base(timer, serviceScopeFactory)
    {
        _growthStatisticAppService = growthStatisticAppService;
        _logger = logger;
        _referralRefreshTimeOptions = referralRefreshTimeOptions.Value;
        Timer.Period = _referralRefreshTimeOptions.TimePeriod;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        _logger.LogDebug("Sync referral data starting....");
        await _growthStatisticAppService.CalculateReferralRankAsync();
    }
}