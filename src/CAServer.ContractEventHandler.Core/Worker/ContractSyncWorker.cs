using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.ContractEventHandler.Core.Application;
using CAServer.Grains.State.RedPackage;
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


    public ContractSyncWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        IContractAppService contractAppService, IOptions<ContractSyncOptions> workerOptions) : base(timer,
        serviceScopeFactory)
    {
        _contractSyncOptions = workerOptions.Value;
        _contractAppService = contractAppService;
        Timer.Period = 1000 * _contractSyncOptions.Sync;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {

        // await _contractAppService.QueryAndSyncAsync();
        await _contractAppService.Refund(new Guid("b9674a6c-2c71-4dd7-ba12-428a118c253e"));
    }
    

}