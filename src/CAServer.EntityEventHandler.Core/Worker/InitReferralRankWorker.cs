using System.Threading.Tasks;
using CAServer.Growth;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace CAServer.EntityEventHandler.Core.Worker;

public class InitReferralRankWorker : AsyncPeriodicBackgroundWorkerBase
{

    private readonly IGrowthStatisticAppService _growthStatisticAppService;
    
    public InitReferralRankWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory, IGrowthStatisticAppService growthStatisticAppService) : base(timer, serviceScopeFactory)
    {
        _growthStatisticAppService = growthStatisticAppService;
        Timer.Period = WorkerConst.InitReferralTimePeriod;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        await _growthStatisticAppService.InitReferralRankAsync();
    }
}