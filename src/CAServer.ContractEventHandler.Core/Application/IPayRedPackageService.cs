using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CAServer.Grains.Grain.ApplicationHandler;
using CAServer.Grains.Grain.RedPackage;
using Hangfire;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Orleans;

namespace CAServer.ContractEventHandler.Core.Application;

public interface IPayRedPackageService
{
    Task PayRedPackageAsync(Guid redPackageId);
}

public class PayRedPackageService : IPayRedPackageService
{
    private readonly ILogger<PayRedPackageService> _logger;
    private readonly IClusterClient _clusterClient;
    private readonly PayRedPackageAccount _packageAccount;
    private readonly IContractProvider _contractProvider;
    private readonly GrabRedPackageOptions _grabRedPackageOptions;

    public PayRedPackageService(ILogger<PayRedPackageService> logger,
        IClusterClient clusterClient,
        IOptionsSnapshot<PayRedPackageAccount> packageAccount,
        IContractProvider contractProvider,
        IOptionsSnapshot<GrabRedPackageOptions> grabRedPackageOptions)
    {
        _logger = logger;
        _clusterClient = clusterClient;
        _contractProvider = contractProvider;
        _packageAccount = packageAccount.Value;
        _grabRedPackageOptions = grabRedPackageOptions.Value;
    }

    public Task PayRedPackageAsync(Guid redPackageId)
    {
        RecurringJob.AddOrUpdate(redPackageId.ToString(), () => SendRedPackageAsync(redPackageId),
            _grabRedPackageOptions.Corn);
        _logger.LogInformation("add or update grab RecurringJob, redPackageId:{redPackageId}", redPackageId);

        return Task.CompletedTask;
    }


    // todo: when to stop job
    public async Task SendRedPackageAsync(Guid redPackageId)
    {
        try
        {
            _logger.LogInformation("SendRedPackage start, redPackageId:{redPackageId}", redPackageId);
            var watcher = Stopwatch.StartNew();
            var startTime = DateTime.Now.Ticks;

            _logger.LogInformation("PayRedPackageAsync start and the redPackage id is {redPackageId}",
                redPackageId.ToString());
            var grain = _clusterClient.GetGrain<ICryptoBoxGrain>(redPackageId);
            var redPackageDetail = await grain.GetRedPackage(redPackageId);
            var grabItems = redPackageDetail.Data.Items.Where(t => !t.PaymentCompleted).ToList();
            var payRedPackageFrom = _packageAccount.getOneAccountRandom();
            _logger.LogInformation("red package payRedPackageFrom, payRedPackageFrom is {payRedPackageFrom} ",
                payRedPackageFrom);
            if (grabItems.IsNullOrEmpty())
            {
                _logger.LogInformation("there are no one claim the red packages,red package id is {redPackageId}",
                    redPackageId.ToString());
                return;
            }

            redPackageDetail.Data.Items = grabItems;
            var res = await _contractProvider.SendTransferRedPacketToChainAsync(redPackageDetail, payRedPackageFrom);
            _logger.LogInformation("SendTransferRedPacketToChainAsync result is {res}",
                JsonConvert.SerializeObject(res));

            if (res.TransactionResultDto.Status != TransactionState.Mined)
            {
                _logger.LogError("PayRedPackageAsync fail: " + "\n{res}",
                    JsonConvert.SerializeObject(res, Formatting.Indented));
                return;
            }

            //if success, update the payment status of red package 
            var updateResult = await grain.UpdateRedPackage(grabItems);
            if (!updateResult.Success)
            {
                _logger.LogError("update the payment status of red package fail, redPackage id is {redPackageId}",
                    redPackageId.ToString());
                return;
            }

            _logger.LogInformation("PayRedPackageAsync end and the redPackage id is {redPackageId}",
                redPackageId.ToString());

            var redPackageStatus = updateResult.Data.Status;
            if (redPackageStatus == RedPackageStatus.Expired ||
                redPackageStatus == RedPackageStatus.FullyClaimed ||
                redPackageStatus == RedPackageStatus.Cancelled)
            {
                RemoveRedPackageJob(redPackageId);
            }

            watcher.Stop();
            _logger.LogInformation("#monitor# payRedPackage:{redPackage}, {cost}, {endTime}:", redPackageId.ToString(),
                watcher.Elapsed.Milliseconds.ToString(), (startTime / TimeSpan.TicksPerMillisecond).ToString());
        }
        catch (Exception e)
        {
            _logger.LogError(e, "PayRedPackage error, packageId:{redPackageId}", redPackageId);
        }
    }

    private void RemoveRedPackageJob(Guid redPackageId)
    {
        RecurringJob.RemoveIfExists(redPackageId.ToString());
        _logger.LogInformation("stop send redPackage job, redPackageId:{redPackageId}", redPackageId);
    }
}