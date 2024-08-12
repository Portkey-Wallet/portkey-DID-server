using System.Threading.Tasks;
using CAServer.Growth;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace CAServer.EntityEventHandler.Core.Worker;

public class TonGiftsValidateWorker : AsyncPeriodicBackgroundWorkerBase
{
    private readonly IGrowthStatisticAppService _growthStatisticAppService;
    private readonly ILogger<TonGiftsValidateWorker> _logger;

    public TonGiftsValidateWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        IGrowthStatisticAppService growthStatisticAppService, ILogger<TonGiftsValidateWorker> logger) : base(timer,
        serviceScopeFactory)
    {
        _growthStatisticAppService = growthStatisticAppService;
        _logger = logger;
        Timer.Period = WorkerConst.InitReferralTimePeriod;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        _logger.LogDebug("Init referral data starting....");
        await _growthStatisticAppService.TonGiftsValidateAsync();
    }
}