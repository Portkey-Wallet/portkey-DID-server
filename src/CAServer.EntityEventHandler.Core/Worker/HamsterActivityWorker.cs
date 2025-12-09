using System.Threading.Tasks;
using CAServer.Growth;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace CAServer.EntityEventHandler.Core.Worker;

public class HamsterActivityWorker : AsyncPeriodicBackgroundWorkerBase
{
    private readonly ILogger<HamsterActivityWorker> _logger;
    private readonly IGrowthStatisticAppService _growthStatisticAppService;

    public HamsterActivityWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        ILogger<HamsterActivityWorker> logger, IGrowthStatisticAppService growthStatisticAppService) : base(timer, serviceScopeFactory)
    {
        _logger = logger;
        _growthStatisticAppService = growthStatisticAppService;
        Timer.Period = WorkerConst.TimePeriod;
    }


    protected  override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        _logger.LogDebug("Hamster Referral Data Calculate Begin....");
        await _growthStatisticAppService.CalculateHamsterDataAsync();

    }
}