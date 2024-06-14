using System.Threading.Tasks;
using CAServer.Growth;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace CAServer.EntityEventHandler.Core.Worker;

public class ReferralRankWorker : AsyncPeriodicBackgroundWorkerBase
{

    private readonly IGrowthStatisticAppService _growthStatisticAppService;
    
    public ReferralRankWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory, IGrowthStatisticAppService growthStatisticAppService) : base(timer, serviceScopeFactory)
    {
        _growthStatisticAppService = growthStatisticAppService;
        Timer.Period = WorkerConst.TimePeriod;

    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        await _growthStatisticAppService.CalculateReferralRankAsync();
    }
}