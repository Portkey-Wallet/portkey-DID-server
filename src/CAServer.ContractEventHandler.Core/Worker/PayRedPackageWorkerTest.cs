using System;
using System.Threading.Tasks;
using CAServer.ContractEventHandler.Core;
using CAServer.ContractEventHandler.Core.Application;
using CAServer.Grains.Grain.ApplicationHandler;
using CAServer.Grains.Grain.RedPackage;
using CAServer.RedPackage.Dtos;
using CAServer.RedPackage.Etos;
using Newtonsoft.Json;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Runtime;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.Threading;
using IContractProvider = CAServer.Common.IContractProvider;

public class PayRedPackageWorkerTest : AsyncPeriodicBackgroundWorkerBase
{
    private readonly IContractAppService _contractAppService;
    private readonly ContractSyncOptions _contractSyncOptions;
    private readonly IClusterClient _clusterClient;
    private readonly ILogger<PayRedPackageTask> _logger;
    private readonly IContractProvider _contractProvider;
    private readonly PayRedPackageAccount _packageAccount;
    private readonly IDistributedEventBus _distributedEventBus;


    public PayRedPackageWorkerTest(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        IContractAppService contractAppService, IOptions<ContractSyncOptions> workerOptions, ILogger<PayRedPackageTask> logger, 
        IContractProvider contractProvider, IClusterClient clusterClient) : base(timer,
        serviceScopeFactory)
    {
        _contractSyncOptions = workerOptions.Value;
        _contractAppService = contractAppService;
        _logger = logger;
        _contractProvider = contractProvider;
        _clusterClient = clusterClient;
        Timer.Period = 1000;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        Console.Write("daiyabin");
        // var redPackageId = new Guid("2e80e55e-5552-4089-a3f2-bbf9a3a0c021");
        // var grain = _clusterClient.GetGrain<RedPackageGrain>(redPackageId);
        //
        // var redPackageDetail = await grain.GetRedPackage(redPackageId);
        // var grabItems = redPackageDetail.Data.Items;
        //
        //if red package expire we should refund it
        
        // var payRedPackageFrom = _packageAccount.getOneAccountRandom();
        // var res = await _contractProvider.SendTransferRedPacketToChainAsync(redPackageDetail,payRedPackageFrom);
    }
}