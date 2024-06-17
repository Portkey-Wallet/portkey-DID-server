using System.Threading.Tasks;
using CAServer.Growth;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace CAServer.EntityEventHandler.Core.Worker;

public class ReferralRankWorker : AsyncPeriodicBackgroundWorkerBase
{

    private readonly IGrowthStatisticAppService _growthStatisticAppService;
    private readonly ILogger<ReferralRankWorker> _logger;
    
    public ReferralRankWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory, IGrowthStatisticAppService growthStatisticAppService, ILogger<ReferralRankWorker> logger) : base(timer, serviceScopeFactory)
    {
        _growthStatisticAppService = growthStatisticAppService;
        _logger = logger;
        Timer.Period = WorkerConst.TimePeriod;

    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        _logger.LogDebug("Sync referral data starting....");
        await _growthStatisticAppService.CalculateReferralRankAsync();
    }
}