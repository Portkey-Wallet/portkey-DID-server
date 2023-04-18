using CAServer.CAActivity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace CAServer.BackGround.Worker;

public class TransactionPullWorker : AsyncPeriodicBackgroundWorkerBase
{
    private readonly IUserActivityAppService _userActivityAppService;

    public TransactionPullWorker(
        IUserActivityAppService userActivityAppService,
        AbpAsyncTimer timer,
        IServiceScopeFactory serviceScopeFactory
        ) : base(timer, serviceScopeFactory)
    {
        _userActivityAppService = userActivityAppService;
        Timer.Period = 1000;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
         await TransactionPullAsync();
    }

    private async Task TransactionPullAsync()
    {
    }
}