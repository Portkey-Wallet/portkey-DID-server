using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CAServer.ContractEventHandler.Core.Application;
using CAServer.Grains.Grain.ApplicationHandler;
using CAServer.Grains.Grain.RedPackage;
using CAServer.RedPackage.Dtos;
using CAServer.RedPackage.Etos;
using Newtonsoft.Json;
using Hangfire;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Runtime;
using Volo.Abp.EventBus.Distributed;
using IContractProvider = CAServer.Common.IContractProvider;

namespace CAServer.ContractEventHandler.Core;


public interface IPayRedPackageTask
{
    public Task PayRedPackageAsync(Guid input);
}

public class PayRedPackageTask : IPayRedPackageTask
{
    private readonly IClusterClient _clusterClient;
    private readonly ILogger<PayRedPackageTask> _logger;
    private readonly IContractProvider _contractProvider;
    private readonly PayRedPackageAccount _packageAccount;
    private readonly IDistributedEventBus _distributedEventBus;

    

    public PayRedPackageTask(IClusterClient clusterClient, ILogger<PayRedPackageTask> logger, 
         IOptionsSnapshot<PayRedPackageAccount> packageAccount, 
         IContractProvider contractProvider, 
         IDistributedEventBus distributedEventBus)
    {
        _clusterClient = clusterClient;
        _logger = logger;
        _packageAccount = packageAccount.Value;
        _contractProvider = contractProvider;
        _distributedEventBus = distributedEventBus;
    }

    [Queue("redpackage")]
    public async Task PayRedPackageAsync(Guid redPackageId)
    {
        _logger.Info("PayRedPackageAsync start and the redpackage id is {}",redPackageId);
        var grain = _clusterClient.GetGrain<RedPackageGrain>(redPackageId);

        var redPackageDetail = await grain.GetRedPackage(redPackageId);
        var grabItems = redPackageDetail.Data.Items;
        var payRedPackageFrom = _packageAccount.getOneAccountRandom();

        //if red package expire we should refund it
        if (await Refund(redPackageDetail.Data, grain,payRedPackageFrom))
        { 
            _logger.Info("red package is expired and it has been refunded,red package id is{} ",redPackageId);
            return;
        }
        
        //if we need judge other params ?
        if (grabItems.IsNullOrEmpty())
        {
            _logger.Info("there are no one claim the red packages,red package id is{} ",redPackageId);
        }
        
        var res = await _contractProvider.SendTransferRedPacketToChainAsync(redPackageDetail,payRedPackageFrom);
        var result = res.TransactionResultDto;
        var eto = new RedPackageTransactionResultEto();
        if (result.Status != TransactionState.Mined)
        {
            eto.Message = "Transaction status: " + result.Status + ". Error: " +
                          result.Error;
            eto.Success = false;

            _logger.LogInformation("PayRedPackageAsync pushed: " + "\n{result}",
                JsonConvert.SerializeObject(eto, Formatting.Indented));

            await _distributedEventBus.PublishAsync(eto);
            return;
        }
            
        if (!result.Logs.Select(l => l.Name).Contains(LogEvent.TransferRedPacket))
        {
            eto.Message = "Transaction status: FAILED" + ". Error: Verification failed";
            eto.Success = false;

            _logger.LogInformation("PayRedPackageAsync pushed: " + "\n{result}",
                JsonConvert.SerializeObject(eto, Formatting.Indented));

            await _distributedEventBus.PublishAsync(eto);
            return;
        }
        //if success update the payment status of red package 
        await grain.UpdateRedPackage(grabItems); 
        _logger.Info("PayRedPackageAsync end and the redpackage id is {}",redPackageId);
        eto.Message = "Transaction status: " + result.Status;
        await _distributedEventBus.PublishAsync(eto);
    }

    private async Task<bool> Refund(RedPackageDetailDto redPackageDetail,RedPackageGrain grain,string payRedPackageFrom )
    {
        if (redPackageDetail == null || grain == null)
        {
            return false;
        }

        if (redPackageDetail.Status.Equals(RedPackageStatus.Expired) && !redPackageDetail.IsRedPackageFullyClaimed)
        {
            var res = await _contractProvider.SendTransferRedPacketRefundAsync(redPackageDetail,payRedPackageFrom);
            await grain.UpdateRedPackageExpire();
            return true;
        }

        return false ;
    }
}