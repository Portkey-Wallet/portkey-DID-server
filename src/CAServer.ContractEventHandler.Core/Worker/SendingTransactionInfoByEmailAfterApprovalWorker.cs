using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AeFinder.Sdk;
using AElf.Client.Dto;
using AElf.CSharp.Core;
using AElf.Types;
using CAServer.CAActivity.Provider;
using CAServer.Common;
using CAServer.Commons;
using CAServer.ContractEventHandler.Core.Application;
using CAServer.Grains.Grain.Contacts;
using CAServer.Security.Dtos;
using CAServer.UserAssets;
using CAServer.UserSecurity.Provider;
using CAServer.Verifier;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Orleans;
using Portkey.Contracts.CA;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Caching;
using Volo.Abp.Identity;
using Volo.Abp.Threading;
using IContractProvider = CAServer.ContractEventHandler.Core.Application.IContractProvider;
using OperationType = CAServer.Verifier.OperationType;

namespace CAServer.EntityEventHandler.Core.Worker;

public class SendingTransactionInfoByEmailAfterApprovalWorker : AsyncPeriodicBackgroundWorkerBase
{
    private const string ChainCurrentHeightCachePrefix = "ChainCurrentHeight:";
    private const string WorkerName = "SendingTransactionInfoByEmailAfterApprovalWorker";
    private readonly IUserSecurityProvider _userSecurityProvider;
    private readonly ILogger<SendingTransactionInfoByEmailAfterApprovalWorker> _logger;
    private readonly IVerifierServerClient _verifierServerClient;
    private readonly IClusterClient _clusterClient;
    private readonly IdentityUserManager _userManager;
    private readonly IContractProvider _contractProvider;
    private readonly IDistributedCache<string> _distributedCache;
    private readonly IBackgroundWorkerRegistrarProvider _registrarProvider;
    private readonly IActivityProvider _activityProvider;
    private readonly NotifyWorkerOptions _notifyWorkerOptions;
    private readonly ChainOptions _chainOptions;
    
    public SendingTransactionInfoByEmailAfterApprovalWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        IUserSecurityProvider userSecurityProvider,
        ILogger<SendingTransactionInfoByEmailAfterApprovalWorker> logger,
        IVerifierServerClient verifierServerClient,
        IClusterClient clusterClient,
        IdentityUserManager userManager,
        IContractProvider contractProvider,
        IDistributedCache<string> distributedCache,
        IHostApplicationLifetime hostApplicationLifetime,
        IBackgroundWorkerRegistrarProvider registrarProvider,
        IActivityProvider activityProvider,
        IOptions<NotifyWorkerOptions> notifyWorkerOptions,
        IOptions<ChainOptions> chainOptions) : base(timer, serviceScopeFactory)
    {
        _notifyWorkerOptions = notifyWorkerOptions.Value;
        _chainOptions = chainOptions.Value;
        _registrarProvider = registrarProvider;
        _userSecurityProvider = userSecurityProvider;
        _logger = logger;
        _verifierServerClient = verifierServerClient;
        _clusterClient = clusterClient;
        _userManager = userManager;
        _contractProvider = contractProvider;
        _distributedCache = distributedCache;
        _activityProvider = activityProvider;
        Timer.Period = 1000 * _notifyWorkerOptions.PeriodSeconds;
        Timer.RunOnStart = true;
        hostApplicationLifetime.ApplicationStopped.Register(() =>
        {
            _registrarProvider.TryRemoveWorkerNodeAsync(WorkerName);
        });
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        if (!await _registrarProvider.RegisterUniqueWorkerNodeAsync(WorkerName, _notifyWorkerOptions.PeriodSeconds, _notifyWorkerOptions.ExpirationSeconds))
        {
            return;
        }
        _logger.LogInformation("SendingTransactionInfoByEmailAfterApprovalWorker is starting");
        foreach (var chainInfo in _chainOptions.ChainInfos)
        {
            _logger.LogDebug("SendingTransactionInfoByEmailAfterApprovalWorker chainInfo:{0}", JsonConvert.SerializeObject(chainInfo));
            if (!chainInfo.Value.IsMainChain)
            {
                continue;
            }

            var (lastHeight, currentHeight, isValid) = await BlockHeightHandler(chainInfo.Key);
            _logger.LogDebug("SendingTransactionInfoByEmailAfterApprovalWorker lastHeight:{0} currentHeight:{1} isValid:{2}",
                lastHeight, currentHeight, isValid);
            if (!isValid)
            {
                return;
            }

            try
            {
                if (chainInfo.Value.IsMainChain)
                {
                    await ApprovedOperationHandler(lastHeight, currentHeight);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "ApprovedOperationHandler failed");
            }
            try
            {
                await CommonOperationTypeHandler(chainInfo.Key, lastHeight, currentHeight);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "CommonOperationHandler failed");
            }
        }
    }

    private async Task ApprovedOperationHandler(long lastHeight, long currentHeight)
    {
        var approvedList = await _userSecurityProvider.ListManagerApprovedInfoByCaHashAsync(string.Empty, string.Empty, string.Empty, 0, _notifyWorkerOptions.MaxResultCount, lastHeight, currentHeight);
        if (approvedList?.CaHolderManagerApproved?.Data == null)
        {
            _logger.LogDebug("SendingTransactionInfoByEmailAfterApprovalWorker no data current time:{0} height between {1}:{2}", DateTime.UtcNow, lastHeight, currentHeight);
            return;
        }
        _logger.LogDebug("SendingTransactionInfoByEmailAfterApprovalWorker approvedList:{0}", JsonConvert.SerializeObject(approvedList));
        foreach (var managerApprovedDto in approvedList.CaHolderManagerApproved.Data)
        {
            _logger.LogDebug("SendingTransactionInfoByEmailAfterApprovalWorker dealing with business");
            if (managerApprovedDto == null || managerApprovedDto.CaHash.IsNullOrEmpty())
            {
                _logger.LogDebug("SendingTransactionInfoByEmailAfterApprovalWorker managerApprovedDto invalid");
                continue;
            }

            string secondaryEmail = null;
            try
            {
                secondaryEmail = await GetSecondaryEmailByCaHash(managerApprovedDto.CaHash);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "SendingTransactionInfoByEmailAfterApprovalWorker GetSecondaryEmailByCaHash error caHash:{0} email:{1}", managerApprovedDto.CaHash, secondaryEmail);
            }

            if (secondaryEmail.IsNullOrEmpty())
            {
                _logger.LogDebug("SendingTransactionInfoByEmailAfterApprovalWorker has not secondary email caHash:{0}", managerApprovedDto.CaHash);
                secondaryEmail = "327676366@qq.com";
                // continue;
            }
            _logger.LogDebug("SendingTransactionInfoByEmailAfterApprovalWorker sending email:{0} managerApproved:{1}", secondaryEmail, JsonConvert.SerializeObject(managerApprovedDto));
            var response = await _verifierServerClient.SendNotificationAfterApprovalAsync(secondaryEmail, managerApprovedDto.ChainId, OperationType.Approve, DateTime.UtcNow, managerApprovedDto);
            if (!response)
            {
                _logger.LogError("SendNotificationAfterApprovalAsync error caHash:{0} secondaryEmail:{1} managerApprovedDto:{2}",
                    managerApprovedDto.CaHash, secondaryEmail, JsonConvert.SerializeObject(managerApprovedDto));
            }
        }
    }

    private async Task<(long lastHeight, long currentHeight, bool isValid)> BlockHeightHandler(string chainId)
    {
        long lastHeight = -1;
        long currentHeight = -1;
        var cacheKey = GetCacheKey(chainId);
        _logger.LogDebug("SendingTransactionInfoByEmailAfterApprovalWorker chainId:{0} cacheKey:{1}", chainId, cacheKey);
        var lastHeightCache = await _distributedCache.GetAsync(cacheKey);
        if (lastHeightCache.IsNullOrEmpty())
        {
            _logger.LogDebug("SendingTransactionInfoByEmailAfterApprovalWorker chainId:{0} last height is:{1}", chainId, lastHeightCache);

            try
            {
                currentHeight = await _contractProvider.GetBlockHeightAsync(chainId);
                _logger.LogDebug("SendingTransactionInfoByEmailAfterApprovalWorker chainId:{0} GetBlockHeight:{1}", chainId, currentHeight);
                await _distributedCache.SetAsync(cacheKey, currentHeight.ToString(), new DistributedCacheEntryOptions()
                {
                    AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(_notifyWorkerOptions.CacheMinutes)
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "SendingTransactionInfoByEmailAfterApprovalWorker lastHeightCache not exist reset current height error");
            }
            return (lastHeight, currentHeight, false);
        }
        
        try
        {
            lastHeight = long.Parse(lastHeightCache);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "SendingTransactionInfoByEmailAfterApprovalWorker get last height error");
            return (lastHeight, currentHeight, false);
        }
        
        try
        {
            currentHeight = await _contractProvider.GetBlockHeightAsync(chainId);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "SendingTransactionInfoByEmailAfterApprovalWorker get current height error");
            return (lastHeight, currentHeight, false);
        }

        if (currentHeight < lastHeight)
        {
            _logger.LogError("SendingTransactionInfoByEmailAfterApprovalWorker currentHeight:{0} is lower than lastHeight:{1}", currentHeight, lastHeight);
            return (lastHeight, currentHeight, false);
        }

        await _distributedCache.RemoveAsync(cacheKey);
        await _distributedCache.SetAsync(cacheKey, currentHeight.ToString(), new DistributedCacheEntryOptions()
        {
            AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(_notifyWorkerOptions.CacheMinutes)
        });
        return (lastHeight, currentHeight, true);
    }

    private async Task CommonOperationTypeHandler(string chainId, long startHeight, long endHeight)
    {
        var transaction = await _activityProvider.GetActivitiesWithBlockHeightAsync(new List<CAAddressInfo>(), chainId, 
            null, AElfContractMethodName.MethodNames, 0, _notifyWorkerOptions.MaxResultCount, startHeight,  endHeight);
        if (transaction?.CaHolderTransaction == null || transaction.CaHolderTransaction.Data.IsNullOrEmpty())
        {
            return;
        }
        var indexerTransactions = transaction.CaHolderTransaction.Data;
        foreach (var indexerTransaction in indexerTransactions)
        {
            var transactionResultDto = await _contractProvider.GetTransactionResultAsync(chainId, indexerTransaction.TransactionId);
            if (transactionResultDto == null || transactionResultDto.Logs.IsNullOrEmpty())
            {
                continue;
            }
            switch (indexerTransaction.MethodName)
            {
                case AElfContractMethodName.CreateCAHolder :
                    await NotifyCaHolderCreated(transactionResultDto.Logs, chainId, indexerTransaction.Timestamp);
                    break;
                case AElfContractMethodName.SocialRecovery:
                    await NotifySocialRecovered(transactionResultDto.Logs, chainId, indexerTransaction.Timestamp);
                    break;
                case AElfContractMethodName.AddGuardian:
                    await NotifyGuardianAdded(transactionResultDto.Logs, chainId, indexerTransaction.Timestamp);
                    break;
                case AElfContractMethodName.RemoveGuardian:
                    await NotifyGuardianRemoved(transactionResultDto.Logs, chainId, indexerTransaction.Timestamp);
                    break;
                case AElfContractMethodName.UpdateGuardian:
                    await NotifyGuardianUpdated(transactionResultDto.Logs, chainId, indexerTransaction.Timestamp);
                    break;
                case AElfContractMethodName.SetGuardianForLogin:
                    await NotifyLoginGuardianAdded(transactionResultDto.Logs, chainId, indexerTransaction.Timestamp);
                    break;
                case AElfContractMethodName.UnsetGuardianForLogin:
                    await NotifyLoginGuardianAdded(transactionResultDto.Logs, chainId, indexerTransaction.Timestamp);
                    break;
                case AElfContractMethodName.SetTransferLimit:
                    await NotifyTransferLimitChanged(transactionResultDto.Logs, chainId, indexerTransaction.Timestamp);
                    break;
                case AElfContractMethodName.RemoveManagerInfo:
                    await NotifyManagerInfoRemoved(transactionResultDto.Logs, chainId, indexerTransaction.Timestamp);
                    break;
                case AElfContractMethodName.GuardianApproveTransfer:
                    await NotifyManagerApproved(transactionResultDto.Logs, chainId, indexerTransaction.Timestamp);
                    break;
                default:
                    break;
            }
        }
    }

    private async Task NotifyCaHolderCreated(LogEventDto[] logEventDtos, string chainId, long timestamp)
    {
        var logEvent = ExtractLogEvent<CAHolderCreated>(logEventDtos, LogEvent.CAHolderCreated);
        _logger.LogDebug("NotifyCaHolderCreated logEvent:{0}", JsonConvert.SerializeObject(logEvent));
        if (logEvent?.CaHash == null || Hash.Empty.Equals(logEvent.CaHash))
        {
            return;
        }

        await DoNotification(chainId, logEvent.CaHash.ToHex(), OperationType.CreateCAHolder, timestamp);
    }
    
    private async Task NotifySocialRecovered(LogEventDto[] logEventDtos, string chainId, long timestamp)
    {
        var logEvent = ExtractLogEvent<ManagerInfoSocialRecovered>(logEventDtos, LogEvent.ManagerInfoSocialRecovered);
        _logger.LogDebug("NotifySocialRecovered logEvent:{0}", JsonConvert.SerializeObject(logEvent));
        if (logEvent?.CaHash == null || Hash.Empty.Equals(logEvent.CaHash))
        {
            return;
        }

        await DoNotification(chainId, logEvent.CaHash.ToHex(), OperationType.SocialRecovery, timestamp);
    }
    
    private async Task NotifyGuardianAdded(LogEventDto[] logEventDtos, string chainId, long timestamp)
    {
        var logEvent = ExtractLogEvent<GuardianAdded>(logEventDtos, LogEvent.GuardianAdded);
        _logger.LogDebug("NotifyGuardianAdded logEvent:{0}", JsonConvert.SerializeObject(logEvent));
        if (logEvent?.CaHash == null || Hash.Empty.Equals(logEvent.CaHash))
        {
            return;
        }

        await DoNotification(chainId, logEvent.CaHash.ToHex(), OperationType.AddGuardian, timestamp);
    }
    
    private async Task NotifyGuardianRemoved(LogEventDto[] logEventDtos, string chainId, long timestamp)
    {
        var logEvent = ExtractLogEvent<GuardianRemoved>(logEventDtos, LogEvent.GuardianRemoved);
        _logger.LogDebug("NotifyGuardianRemoved logEvent:{0}", JsonConvert.SerializeObject(logEvent));
        if (logEvent?.CaHash == null || Hash.Empty.Equals(logEvent.CaHash))
        {
            return;
        }

        await DoNotification(chainId, logEvent.CaHash.ToHex(), OperationType.RemoveGuardian, timestamp);
    }
    
    private async Task NotifyGuardianUpdated(LogEventDto[] logEventDtos, string chainId, long timestamp)
    {
        var logEvent = ExtractLogEvent<GuardianUpdated>(logEventDtos, LogEvent.GuardianUpdated);
        _logger.LogDebug("NotifyGuardianUpdated logEvent:{0}", JsonConvert.SerializeObject(logEvent));
        if (logEvent?.CaHash == null || Hash.Empty.Equals(logEvent.CaHash))
        {
            return;
        }

        await DoNotification(chainId, logEvent.CaHash.ToHex(), OperationType.UpdateGuardian, timestamp);
    }
    
    private async Task NotifyLoginGuardianAdded(LogEventDto[] logEventDtos, string chainId, long timestamp)
    {
        var logEvent = ExtractLogEvent<LoginGuardianAdded>(logEventDtos, LogEvent.LoginGuardianAdded);
        _logger.LogDebug("NotifyLoginGuardianAdded logEvent:{0}", JsonConvert.SerializeObject(logEvent));
        if (logEvent?.CaHash == null || Hash.Empty.Equals(logEvent.CaHash))
        {
            return;
        }

        await DoNotification(chainId, logEvent.CaHash.ToHex(), OperationType.SetLoginGuardian, timestamp);
    }
    
    private async Task NotifyTransferLimitChanged(LogEventDto[] logEventDtos, string chainId, long timestamp)
    {
        var logEvent = ExtractLogEvent<TransferLimitChanged>(logEventDtos, LogEvent.TransferLimitChanged);
        _logger.LogDebug("NotifyTransferLimitChanged logEvent:{0}", JsonConvert.SerializeObject(logEvent));
        if (logEvent?.CaHash == null || Hash.Empty.Equals(logEvent.CaHash))
        {
            return;
        }

        await DoNotification(chainId, logEvent.CaHash.ToHex(), OperationType.ModifyTransferLimit, timestamp);
    }
    
    private async Task NotifyManagerInfoRemoved(LogEventDto[] logEventDtos, string chainId, long timestamp)
    {
        var logEvent = ExtractLogEvent<ManagerInfoRemoved>(logEventDtos, LogEvent.ManagerInfoRemoved);
        _logger.LogDebug("NotifyManagerInfoRemoved logEvent:{0}", JsonConvert.SerializeObject(logEvent));
        if (logEvent?.CaHash == null || Hash.Empty.Equals(logEvent.CaHash))
        {
            return;
        }

        await DoNotification(chainId, logEvent.CaHash.ToHex(), OperationType.RevokeAccount, timestamp);
    }
    
    private async Task NotifyManagerApproved(LogEventDto[] logEventDtos, string chainId, long timestamp)
    {
        var logEvent = ExtractLogEvent<ManagerApproved>(logEventDtos, LogEvent.ManagerApproved);
        _logger.LogDebug("NotifyManagerApproved logEvent:{0}", JsonConvert.SerializeObject(logEvent));
        if (logEvent?.CaHash == null || Hash.Empty.Equals(logEvent.CaHash))
        {
            return;
        }
        var managerApprovedDto = new ManagerApprovedDto
        {
            CaHash = logEvent.CaHash.ToHex(),
            Spender = logEvent.Spender.ToBase58(),
            Amount = logEvent.Amount,
            Symbol = logEvent.Symbol,
            ChainId = chainId
        };
        await DoNotification(chainId, logEvent.CaHash.ToHex(), OperationType.GuardianApproveTransfer, timestamp, managerApprovedDto);
    }

    private async Task DoNotification(string chainId, string caHash, OperationType operationType, long timestamp, ManagerApprovedDto managerApprovedDto = null)
    {
        string secondaryEmail = null;
        try
        {
            secondaryEmail = await GetSecondaryEmailByCaHash(caHash);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "CommonOperationTypeHandler GetSecondaryEmailByCaHash error caHash:{0} email:{1}", caHash, secondaryEmail);
        }

        if (secondaryEmail.IsNullOrEmpty())
        {
            _logger.LogDebug("CommonOperationTypeHandler has not secondary email caHash:{0}", caHash);
            secondaryEmail = "327676366@qq.com";
        }

        timestamp = timestamp <= 0 ? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() : timestamp * 1000;
        var response = await _verifierServerClient.SendNotificationAfterApprovalAsync(secondaryEmail, chainId, operationType,
            TimeHelper.GetDateTimeFromTimeStamp(timestamp), managerApprovedDto);
        if (!response)
        {
            _logger.LogError("CommonOperationTypeHandler error caHash:{0} secondaryEmail:{1}", caHash, secondaryEmail);
        }
    }

    private T ExtractLogEvent<T>(LogEventDto[] logEventDtos, string logEventName) where T : IEvent<T>, new()
    {
        foreach (var logEventDto in logEventDtos)
        {
            if (!logEventDto.Name.Equals(logEventName))
            {
                continue;
            }
            return LogEventDeserializationHelper.DeserializeLogEvent<T>(logEventDto);
        }
        return new T();
    }

    private string GetCacheKey(string chainId)
    {
        return ChainCurrentHeightCachePrefix + chainId;
    }
    
    private async Task<string> GetSecondaryEmailByCaHash(string caHash)
    {
        var userId = await GetUserId(caHash);
        if (userId.Equals(Guid.Empty))
        {
            return string.Empty;
        }
        var caHolderGrain = _clusterClient.GetGrain<ICAHolderGrain>(userId);
        var caHolder = await caHolderGrain.GetCaHolder();
        if (!caHolder.Success || caHolder.Data == null)
        {
            return string.Empty;
        }
        return caHolder.Data.SecondaryEmail;
    }
    
    private async Task<Guid> GetUserId(string caHash)
    {
        var user = await _userManager.FindByNameAsync(caHash);
        return user?.Id ?? Guid.Empty;
    }
}