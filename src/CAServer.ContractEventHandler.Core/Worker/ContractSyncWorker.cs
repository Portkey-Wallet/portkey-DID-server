using System;
using System.Threading.Tasks;
using CAServer.ContractEventHandler.Core.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace CAServer.ContractEventHandler.Core.Worker;

public class ContractSyncWorker : AsyncPeriodicBackgroundWorkerBase
{
    private readonly IContractAppService _contractAppService;
    private readonly ContractSyncOptions _contractSyncOptions;
    private readonly ILogger<ContractSyncWorker> _logger;

    public ContractSyncWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        ILogger<ContractSyncWorker> logger,
        IContractAppService contractAppService, IOptions<ContractSyncOptions> workerOptions) : base(timer,
        serviceScopeFactory)
    {
        _contractSyncOptions = workerOptions.Value;
        _contractAppService = contractAppService;
        _logger = logger;
        Timer.Period = 1000 * _contractSyncOptions.Sync;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        _logger.LogInformation("QueryAndSyncAsync start==============");
        try
        {
            await _contractAppService.QueryAndSyncAsync();
        }
        catch (Exception e)
        {
            _logger.LogInformation("QueryAndSyncAsync end exception==============");
        }
       
        _logger.LogInformation("QueryAndSyncAsync end==============");
    }
}