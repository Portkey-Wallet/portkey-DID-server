using System.Threading.Tasks;
using CAServer.Growth;
using CAServer.Options;
using CAServer.ScheduledTask;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CAServer.EntityEventHandler.Core.Worker;

public class ReferralRankWorker : ScheduledTaskBase
{
    private readonly IGrowthStatisticAppService _growthStatisticAppService;
    private readonly ILogger<ReferralRankWorker> _logger;
    private readonly ReferralRefreshTimeOptions _referralRefreshTimeOptions;
    
    public ReferralRankWorker(
        IGrowthStatisticAppService growthStatisticAppService, ILogger<ReferralRankWorker> logger,
        IOptionsSnapshot<ReferralRefreshTimeOptions> referralRefreshTimeOptions)
    {
        _growthStatisticAppService = growthStatisticAppService;
        _logger = logger;
        _referralRefreshTimeOptions = referralRefreshTimeOptions.Value;
        Period = _referralRefreshTimeOptions.TimePeriod;
    }

    protected override async Task DoWorkAsync()
    {
        _logger.LogDebug("Sync referral data starting....");
        await _growthStatisticAppService.CalculateReferralRankAsync();
    }
}