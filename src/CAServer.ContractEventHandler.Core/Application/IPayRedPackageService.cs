using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AElf.Client.Dto;
using AElf.Indexing.Elasticsearch;
using CAServer.Contacts.Provider;
using CAServer.Entities.Es;
using CAServer.EnumType;
using CAServer.Grains.Grain.ApplicationHandler;
using CAServer.Grains.Grain.RedPackage;
using CAServer.RedPackage.Dtos;
using Hangfire;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Orleans;
using Volo.Abp.Caching;
using Volo.Abp.DistributedLocking;

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
    private readonly IDistributedCache<PayRedPackageRecurring> _distributedCache;
    private readonly IDistributedCache<string> _payDistributedCache;
    private readonly IAbpDistributedLock _distributedLock;
    private readonly string _lockKeyPrefix = "CAServer:ContractEventHandler:PayRedPackage:";
    private readonly string _lockPayRedPackagePrefix = "CAServer:ContractEventHandler:LockPayRedPackage:";
    private readonly string _payRedPackageRecurringPrefix = "CAServer:ContractEventHandler:RedPackageRecurring:";
    private readonly INESTRepository<RedPackageIndex, Guid> _redPackageRepository;
    private readonly IContactProvider _contactProvider;

    public PayRedPackageService(ILogger<PayRedPackageService> logger,
        IClusterClient clusterClient,
        IOptionsSnapshot<PayRedPackageAccount> packageAccount,
        IContractProvider contractProvider,
        IOptionsSnapshot<GrabRedPackageOptions> grabRedPackageOptions,
        IDistributedCache<PayRedPackageRecurring> distributedCache,
        IAbpDistributedLock distributedLock, IDistributedCache<string> payDistributedCache,
        INESTRepository<RedPackageIndex, Guid> redPackageRepository,
        IContactProvider contactProvider)
    {
        _logger = logger;
        _clusterClient = clusterClient;
        _contractProvider = contractProvider;
        _distributedCache = distributedCache;
        _distributedLock = distributedLock;
        _payDistributedCache = payDistributedCache;
        _packageAccount = packageAccount.Value;
        _grabRedPackageOptions = grabRedPackageOptions.Value;
        _redPackageRepository = redPackageRepository;
        _contactProvider = contactProvider;
    }

    public async Task PayRedPackageAsync(Guid redPackageId)
    {
        try
        {
            await using var handle =
                await _distributedLock.TryAcquireAsync(name: _lockKeyPrefix + redPackageId);
            if (handle != null)
            {
                var payRecurringCount = 0;
                var recurringKey = _payRedPackageRecurringPrefix + redPackageId;
                var recurringInfo = await _distributedCache.GetAsync(recurringKey);
                _logger.LogInformation("redPackageId:{0} get the recurringInfo result:{1}", redPackageId, recurringInfo);
                if (recurringInfo != null)
                {
                    var totalPayRecurringCount = recurringInfo.TotalPayRecurringCount;
                    payRecurringCount = GetPayRecurringCount(recurringInfo.PayRecurringCount, totalPayRecurringCount);
                    await UpdateRecurringCountAsync(recurringKey, payRecurringCount, totalPayRecurringCount);
                }

                AddOrUpdateRecurringJob(redPackageId, payRecurringCount);
            }
            else
            {
                _logger.LogWarning("do not get lock, keys already exits, redPackageId: {redPackageId}",
                    redPackageId.ToString());
                return;
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "PayRedPackageAsync error, redPackageId: {redPackageId}", redPackageId.ToString());
        }
    }

    public async Task SendRedPackageWithLockAsync(Guid redPackageId)
    {
        try
        {
            _logger.LogInformation("SendRedPackageWithLockAsync, packageId:{redPackageId}", redPackageId);
            var check = await CheckAsync(redPackageId);
            if (!check)
            {
                _logger.LogWarning("do not acquire send red package lock key, packageId:{redPackageId}", redPackageId);
                return;
            }

            await SetPayCacheAsync(redPackageId);
            await SendRedPackageAsync(redPackageId);

            await RemovePayCacheAsync(redPackageId);
            _logger.LogInformation("release send red package lock, packageId:{redPackageId}", redPackageId);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "send red package error, packageId:{redPackageId}", redPackageId);

            await RemovePayCacheAsync(redPackageId);
            _logger.LogInformation("release send red package lock, packageId:{redPackageId}", redPackageId);
        }
    }

    public async Task SendRedPackageAsync(Guid redPackageId)
    {
        try
        {
            await UpdateRecurringCountAsync(redPackageId);
            var watcher = Stopwatch.StartNew();
            var startTime = DateTime.Now.Ticks;

            _logger.LogInformation("PayRedPackageAsync start and the redPackage id is {redPackageId}",
                redPackageId.ToString());
            var grain = _clusterClient.GetGrain<ICryptoBoxGrain>(redPackageId);
            var redPackageDetail = await grain.GetRedPackage(redPackageId);
            
            var redPackageStatus = redPackageDetail.Data.Status;
            if (redPackageStatus == RedPackageStatus.Expired ||
                redPackageStatus == RedPackageStatus.FullyClaimed ||
                redPackageStatus == RedPackageStatus.Cancelled)
            {
                RemoveRedPackageJob(redPackageId);
            }

            var grabItems = redPackageDetail.Data.Items.Where(t => !t.PaymentCompleted).ToList();
            await FilterGrabItems(redPackageId, redPackageDetail.Data.RedPackageDisplayType, redPackageDetail.Data.ChainId, grabItems);
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
            if (! TransactionState.IsStateSuccessful(res.TransactionResultDto.Status))
            {
                _logger.LogError("PayRedPackageAsync fail: " + "\n{res}",
                    JsonConvert.SerializeObject(res, Formatting.Indented));
                await UpdateSendRedPackageTransactionInfo(redPackageDetail.Data.SessionId, res.TransactionResultDto, false);
                return;
            }

            //if success, update the payment status of red package 
            await grain.UpdateRedPackage(grabItems);
            _logger.LogInformation("PayRedPackageAsync end and the redPackage id is {redPackageId}",
                redPackageId.ToString());
            await UpdateSendRedPackageTransactionInfo(redPackageDetail.Data.SessionId, res.TransactionResultDto, true);
            watcher.Stop();
            _logger.LogInformation("#monitor# payRedPackage:{redPackage}, {cost}, {endTime}:", redPackageId.ToString(),
                watcher.Elapsed.Milliseconds.ToString(), (startTime / TimeSpan.TicksPerMillisecond).ToString());
        }
        catch (Exception e)
        {
            _logger.LogError(e, "PayRedPackage error, packageId:{redPackageId}", redPackageId);
        }
    }
    
    private async Task FilterGrabItems(Guid redPackageId, RedPackageDisplayType displayType,
        string chainId, List<GrabItemDto> grabItems)
    {
        if (!RedPackageDisplayType.CryptoGift.Equals(displayType))
        {
            return;
        }
        if (grabItems.IsNullOrEmpty())
        {
            return;
        }
        foreach (var grabItemDto in grabItems.ToList())
        {
            bool existed = false;
            try
            {
                var guardiansDto = await _contactProvider.GetCaHolderInfoByAddressAsync(new List<string>() {grabItemDto.CaAddress}, chainId);
                _logger.LogInformation("redPackageId:{0} filter caholder of cross chain logic chainId:{1} caAddress:{3} guardiansDto:{4}", redPackageId, chainId, grabItemDto.CaAddress, JsonConvert.SerializeObject(guardiansDto)); 
                existed = guardiansDto.CaHolderInfo.Any(guardian => guardian.CaAddress.Equals(grabItemDto.CaAddress) && guardian.ChainId.Equals(chainId));
            }
            catch (Exception e)
            {
                _logger.LogInformation("redPackageId:{0} chainId:{1} caAddress:{3} query guardians error", redPackageId, chainId, grabItemDto.CaAddress);
            }
            if (!existed)
            {
                grabItems.Remove(grabItemDto);
            }
        }
    }

    private async Task UpdateSendRedPackageTransactionInfo(Guid sessionId, TransactionResultDto transactionResultDto, bool transactionSucceed)
    {
        var redPackageIndex = await _redPackageRepository.GetAsync(sessionId);
        if (redPackageIndex == null)
        {
            _logger.LogError("RedPackage PagedResultEto not found: {Message}",
                JsonConvert.SerializeObject(transactionResultDto));
            return;
        }

        if (redPackageIndex.PayedTransactionIds.IsNullOrEmpty())
        {
            redPackageIndex.PayedTransactionIds = transactionResultDto.TransactionId;
        }
        else
        {
            redPackageIndex.PayedTransactionIds = redPackageIndex.PayedTransactionIds + "," + transactionResultDto.TransactionId;
        }
        if (redPackageIndex.PayedTransactionDtoList == null)
        {
            redPackageIndex.PayedTransactionDtoList = new List<RedPackageIndex.PayedTransactionDto>();
        }
        redPackageIndex.PayedTransactionDtoList.Add(new RedPackageIndex.PayedTransactionDto()
        {
            PayedTransactionId = transactionResultDto.TransactionId,
            PayedTransactionStatus = transactionSucceed ? RedPackageTransactionStatus.Success : RedPackageTransactionStatus.Fail,
            PayedTransactionResult = transactionResultDto.Status
        });
        await _redPackageRepository.UpdateAsync(redPackageIndex);
        _logger.LogInformation("redPackageId:{0} PayedRedPackage UpdateRedPackageEs successfully", redPackageIndex.RedPackageId);
    }

    private void RemoveRedPackageJob(Guid redPackageId)
    {
        RecurringJob.RemoveIfExists(redPackageId.ToString());
        _logger.LogInformation("stop send redPackage job, redPackageId:{redPackageId}", redPackageId);
    }

    // if count < 5 10s, 5-15 20s >15 1h
    private string GetCorn(int payRecurringCount)
    {
        if (payRecurringCount < _grabRedPackageOptions.FirstRecurringCount)
        {
            return $"0/{_grabRedPackageOptions.Interval} * * * * ?";
        }

        if (payRecurringCount < _grabRedPackageOptions.SecondRecurringCount)
        {
            return $"0/{_grabRedPackageOptions.Interval * 2} * * * * ?";
        }

        return "0 0 0/1 * * ?";
    }

    private int GetPayRecurringCount(int payRecurringCount, int totalPayRecurringCount)
    {
        if (totalPayRecurringCount >= _grabRedPackageOptions.SecondRecurringCount)
        {
            return _grabRedPackageOptions.FirstRecurringCount - 1;
        }

        return payRecurringCount;
    }

    private void AddOrUpdateRecurringJob(Guid redPackageId, int payRecurringCount)
    {
        var corn = GetCorn(payRecurringCount);

        RecurringJob.AddOrUpdate(redPackageId.ToString(), () => SendRedPackageWithLockAsync(redPackageId),
            corn);
        _logger.LogInformation("add or update grab RecurringJob, redPackageId:{redPackageId}", redPackageId);
    }

    private async Task UpdateRecurringCountAsync(Guid redPackageId)
    {
        var recurringKey = _payRedPackageRecurringPrefix + redPackageId;
        var recurringInfo = await _distributedCache.GetAsync(recurringKey);
        var totalPayRecurringCount = 1;
        var payRecurringCount = 1;

        if (recurringInfo != null)
        {
            totalPayRecurringCount = ++recurringInfo.TotalPayRecurringCount;
            payRecurringCount = ++recurringInfo.PayRecurringCount;
        }

        await UpdateRecurringCountAsync(recurringKey, payRecurringCount, totalPayRecurringCount);
        AddOrUpdateRecurringJob(redPackageId, payRecurringCount);
        _logger.LogInformation(
            "pay red package, packageId:{packageId}, payRecurringCount:{payRecurringCount}, totalPayRecurringCount:{totalPayRecurringCount}",
            redPackageId, payRecurringCount, totalPayRecurringCount);
    }

    private async Task UpdateRecurringCountAsync(string recurringKey, int payRecurringCount, int totalPayRecurringCount)
    {
        await _distributedCache.SetAsync(recurringKey, new PayRedPackageRecurring()
        {
            TotalPayRecurringCount = totalPayRecurringCount,
            PayRecurringCount = payRecurringCount
        }, new DistributedCacheEntryOptions()
        {
            AbsoluteExpiration = DateTimeOffset.Now.AddDays(_grabRedPackageOptions.RecurringInfoExpireDays)
        });
    }

    private async Task<bool> CheckAsync(Guid redPackageId)
    {
        var result = await GetPayCacheAsync(redPackageId);
        return result.IsNullOrEmpty();
    }

    private async Task<string> GetPayCacheAsync(Guid redPackageId)
    {
        var recurringKey = _lockPayRedPackagePrefix + redPackageId;
        return await _payDistributedCache.GetAsync(recurringKey);
    }

    private async Task SetPayCacheAsync(Guid redPackageId)
    {
        var recurringKey = _lockPayRedPackagePrefix + redPackageId;
        await _payDistributedCache.SetAsync(recurringKey, redPackageId.ToString(), new DistributedCacheEntryOptions()
        {
            AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(_grabRedPackageOptions.PayCacheExpireTime)
        });
    }

    private async Task RemovePayCacheAsync(Guid redPackageId)
    {
        var recurringKey = _lockPayRedPackagePrefix + redPackageId;
        await _payDistributedCache.RemoveAsync(recurringKey);
    }
}