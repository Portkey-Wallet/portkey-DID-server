using System.Threading.Tasks;
using CAServer.Cache;
using CAServer.Growth;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace CAServer.EntityEventHandler.Core.Worker;

public class HamsterDataRepairWorker : AsyncPeriodicBackgroundWorkerBase
{
    private readonly ILogger<HamsterDataRepairWorker> _logger;
    private readonly ICacheProvider _cacheProvider;
    private const string RepairDataCache = "Hamster:DataRepairKey";
    private readonly IGrowthStatisticAppService _growthStatisticAppService;


    public HamsterDataRepairWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        ILogger<HamsterDataRepairWorker> logger, ICacheProvider cacheProvider, IGrowthStatisticAppService growthStatisticAppService) : base(timer, serviceScopeFactory)
    {
        _logger = logger;
        _cacheProvider = cacheProvider;
        _growthStatisticAppService = growthStatisticAppService;
        Timer.Period = WorkerConst.InitReferralTimePeriod;
        Timer.RunOnStart = true;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        _logger.LogDebug("Repair Hamster Data Begin");
        var value = await _cacheProvider.Get(RepairDataCache);
        if (value.HasValue)
        {
            _logger.LogDebug("Hamster Data has been repaired.");
            return;
        }
        await _growthStatisticAppService.RepairHamsterDataAsync();


    }
}