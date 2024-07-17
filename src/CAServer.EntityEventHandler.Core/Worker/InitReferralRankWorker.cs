using System.Threading.Tasks;
using CAServer.Growth;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

//namespace CAServer.EntityEventHandler.Core.Worker;

// public class InitReferralRankWorker : AsyncPeriodicBackgroundWorkerBase
// {
//     private readonly IGrowthStatisticAppService _growthStatisticAppService;
//     private readonly ILogger<InitReferralRankWorker> _logger;
//     
//     public InitReferralRankWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory, IGrowthStatisticAppService growthStatisticAppService, ILogger<InitReferralRankWorker> logger) : base(timer, serviceScopeFactory)
//     {
//         _growthStatisticAppService = growthStatisticAppService;
//         _logger = logger;
//         Timer.Period = WorkerConst.InitReferralTimePeriod;
//     }
//
//     protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
//     {
//         _logger.LogDebug("Init referral data starting....");
//         await _growthStatisticAppService.InitReferralRankAsync();
//     }
// }