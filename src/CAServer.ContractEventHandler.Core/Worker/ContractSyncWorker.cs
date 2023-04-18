using System.Threading.Tasks;
using CAServer.ContractEventHandler.Core.Application;
using Microsoft.Extensions.Options;
using Quartz;
using Volo.Abp.BackgroundWorkers.Quartz;

namespace CAServer.ContractEventHandler.Core.Worker;

public class ContractSyncWorker : QuartzBackgroundWorkerBase
{
    private readonly IContractAppService _contractAppService;
    private readonly ContractSyncOptions _contractSyncOptions;

    public ContractSyncWorker(IContractAppService contractAppService, IOptions<ContractSyncOptions> workerOptions)
    {
        _contractSyncOptions = workerOptions.Value;
        _contractAppService = contractAppService;
        JobDetail = JobBuilder.Create<ContractSyncWorker>().WithIdentity(nameof(ContractSyncWorker)).Build();
        Trigger = TriggerBuilder
            .Create()
            .WithIdentity(nameof(ContractSyncWorker))
            .WithCronSchedule(_contractSyncOptions.Cron)
            .StartNow()
            .Build();
    }

    public override async Task Execute(IJobExecutionContext context)
    {
        // await _contractAppService.InitializeIndexAsync(3480000);
        await _contractAppService.QueryEventsAndSyncAsync();
    }
}