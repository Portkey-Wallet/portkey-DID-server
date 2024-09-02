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
using CAServer.Grains.Grain.Contacts;
using CAServer.Security.Dtos;
using CAServer.UserAssets;
using CAServer.UserSecurity.Provider;
using CAServer.Verifier;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
        IActivityProvider activityProvider) : base(timer, serviceScopeFactory)
    {
        _registrarProvider = registrarProvider;
        _userSecurityProvider = userSecurityProvider;
        _logger = logger;
        _verifierServerClient = verifierServerClient;
        _clusterClient = clusterClient;
        _userManager = userManager;
        _contractProvider = contractProvider;
        _distributedCache = distributedCache;
        _activityProvider = activityProvider;
        Timer.Period = 1000 * 10; //10 seconds
        Timer.RunOnStart = true;
        hostApplicationLifetime.ApplicationStopped.Register(() =>
        {
            _registrarProvider.TryRemoveWorkerNodeAsync(WorkerName);
        });
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        //todo move the config to apollo
        if (!await _registrarProvider.RegisterUniqueWorkerNodeAsync(WorkerName, 10, 3600))
        {
            return;
        }
        _logger.LogInformation("SendingTransactionInfoByEmailAfterApprovalWorker is starting");
        var (lastHeight, currentHeight, isValid) = await BlockHeightHandler();
        _logger.LogDebug("SendingTransactionInfoByEmailAfterApprovalWorker lastHeight:{0} currentHeight:{1} isValid:{2}",
            lastHeight, currentHeight, isValid);
        if (!isValid)
        {
            return;
        }

        try
        {
            await ApprovedOperationHandler(lastHeight, currentHeight);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "ApprovedOperationHandler failed");
        }
        try
        {
            await CommonOperationTypeHandler(lastHeight, currentHeight);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "CommonOperationHandler failed");
        }
    }

    private async Task ApprovedOperationHandler(long lastHeight, long currentHeight)
    {
        //todo skip and masResultCount should be put into apollo
        var approvedList = await _userSecurityProvider.ListManagerApprovedInfoByCaHashAsync(string.Empty, string.Empty, string.Empty, 0, 1000, lastHeight, currentHeight);
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

    private async Task<(long lastHeight, long currentHeight, bool isValid)> BlockHeightHandler()
    {
        long lastHeight = -1;
        long currentHeight = -1;
        var cacheKey = GetCacheKey(ContractAppServiceConstant.MainChainId);
        var lastHeightCache = await _distributedCache.GetAsync(cacheKey);
        if (lastHeightCache.IsNullOrEmpty())
        {
            _logger.LogDebug("SendingTransactionInfoByEmailAfterApprovalWorker chainId:{0} last height is:{1}", ContractAppServiceConstant.MainChainId, lastHeight);

            try
            {
                currentHeight = await _contractProvider.GetBlockHeightAsync(ContractAppServiceConstant.MainChainId);
                await _distributedCache.SetAsync(cacheKey, currentHeight.ToString(), new DistributedCacheEntryOptions()
                {
                    AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(1)
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
            currentHeight = await _contractProvider.GetBlockHeightAsync(ContractAppServiceConstant.MainChainId);
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

        await _distributedCache.SetAsync(cacheKey, currentHeight.ToString(), new DistributedCacheEntryOptions()
        {
            AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(1)
        });
        return (lastHeight, currentHeight, true);
    }

    private async Task CommonOperationTypeHandler(long startHeight, long endHeight)
    {
        _logger.LogDebug("=======================CommonOperationTypeHandler starting");
        var transaction = await _activityProvider.GetActivitiesWithBlockHeightAsync(new List<CAAddressInfo>(), ContractAppServiceConstant.MainChainId, 
            string.Empty, AElfContractMethodName.MethodNames, 0, 1000, startHeight,  endHeight);
        _logger.LogDebug("=======================CommonOperationTypeHandler GetActivities startHeight:{0} endHeight:{1} chainId:{2} methods:{3}",
            startHeight,  endHeight, ContractAppServiceConstant.MainChainId, AElfContractMethodName.MethodNames.ToString());
        if (transaction?.CaHolderTransaction == null || transaction.CaHolderTransaction.Data.IsNullOrEmpty())
        {
            return;
        }
        _logger.LogDebug("CommonOperationTypeHandler starting transactionTotalAccount:{0}", transaction.CaHolderTransaction.TotalRecordCount);
        var indexerTransactions = transaction.CaHolderTransaction.Data;
        foreach (var indexerTransaction in indexerTransactions)
        {
            var transactionResultDto = await _contractProvider.GetTransactionResultAsync(ContractAppServiceConstant.MainChainId, indexerTransaction.TransactionId);
            switch (indexerTransaction.MethodName)
            {
                case AElfContractMethodName.CreateCAHolder :
                    await NotifyCaHolderCreated(transactionResultDto.Logs, ContractAppServiceConstant.MainChainId, indexerTransaction.Timestamp);
                    break;
                case AElfContractMethodName.SocialRecovery:
                    await NotifySocialRecovered(transactionResultDto.Logs, ContractAppServiceConstant.MainChainId, indexerTransaction.Timestamp);
                    break;
                case AElfContractMethodName.AddGuardian:
                    await NotifyGuardianAdded(transactionResultDto.Logs, ContractAppServiceConstant.MainChainId, indexerTransaction.Timestamp);
                    break;
                case AElfContractMethodName.RemoveGuardian:
                    await NotifyGuardianRemoved(transactionResultDto.Logs, ContractAppServiceConstant.MainChainId, indexerTransaction.Timestamp);
                    break;
                case AElfContractMethodName.UpdateGuardian:
                    await NotifyGuardianUpdated(transactionResultDto.Logs, ContractAppServiceConstant.MainChainId, indexerTransaction.Timestamp);
                    break;
                case AElfContractMethodName.SetGuardianForLogin:
                    await NotifyLoginGuardianAdded(transactionResultDto.Logs, ContractAppServiceConstant.MainChainId, indexerTransaction.Timestamp);
                    break;
                case AElfContractMethodName.UnsetGuardianForLogin:
                    await NotifyLoginGuardianAdded(transactionResultDto.Logs, ContractAppServiceConstant.MainChainId, indexerTransaction.Timestamp);
                    break;
                case AElfContractMethodName.SetTransferLimit:
                    await NotifyTransferLimitChanged(transactionResultDto.Logs, ContractAppServiceConstant.MainChainId, indexerTransaction.Timestamp);
                    break;
                case AElfContractMethodName.RemoveManagerInfo:
                    await NotifyManagerInfoRemoved(transactionResultDto.Logs, ContractAppServiceConstant.MainChainId, indexerTransaction.Timestamp);
                    break;
                case AElfContractMethodName.GuardianApproveTransfer:
                    await NotifyManagerApproved(transactionResultDto.Logs, ContractAppServiceConstant.MainChainId, indexerTransaction.Timestamp);
                    break;
                default:
                    break;
            }
        }
    }

    private async Task NotifyCaHolderCreated(LogEventDto[] logEventDtos, string chainId, long timestamp)
    {
        var logEvent = ExtractLogEvent<CAHolderCreated>(logEventDtos, LogEvent.CAHolderCreated);
        if (logEvent?.CaHash == null || Hash.Empty.Equals(logEvent.CaHash))
        {
            return;
        }

        await DoNotification(chainId, logEvent.CaHash.ToHex(), OperationType.CreateCAHolder, timestamp);
    }
    
    private async Task NotifySocialRecovered(LogEventDto[] logEventDtos, string chainId, long timestamp)
    {
        var logEvent = ExtractLogEvent<ManagerInfoSocialRecovered>(logEventDtos, LogEvent.ManagerInfoSocialRecovered);
        if (logEvent?.CaHash == null || Hash.Empty.Equals(logEvent.CaHash))
        {
            return;
        }

        await DoNotification(chainId, logEvent.CaHash.ToHex(), OperationType.SocialRecovery, timestamp);
    }
    
    private async Task NotifyGuardianAdded(LogEventDto[] logEventDtos, string chainId, long timestamp)
    {
        var logEvent = ExtractLogEvent<GuardianAdded>(logEventDtos, LogEvent.GuardianAdded);
        if (logEvent?.CaHash == null || Hash.Empty.Equals(logEvent.CaHash))
        {
            return;
        }

        await DoNotification(chainId, logEvent.CaHash.ToHex(), OperationType.AddGuardian, timestamp);
    }
    
    private async Task NotifyGuardianRemoved(LogEventDto[] logEventDtos, string chainId, long timestamp)
    {
        var logEvent = ExtractLogEvent<GuardianRemoved>(logEventDtos, LogEvent.GuardianRemoved);
        if (logEvent?.CaHash == null || Hash.Empty.Equals(logEvent.CaHash))
        {
            return;
        }

        await DoNotification(chainId, logEvent.CaHash.ToHex(), OperationType.RemoveGuardian, timestamp);
    }
    
    private async Task NotifyGuardianUpdated(LogEventDto[] logEventDtos, string chainId, long timestamp)
    {
        var logEvent = ExtractLogEvent<GuardianUpdated>(logEventDtos, LogEvent.GuardianUpdated);
        if (logEvent?.CaHash == null || Hash.Empty.Equals(logEvent.CaHash))
        {
            return;
        }

        await DoNotification(chainId, logEvent.CaHash.ToHex(), OperationType.UpdateGuardian, timestamp);
    }
    
    private async Task NotifyLoginGuardianAdded(LogEventDto[] logEventDtos, string chainId, long timestamp)
    {
        var logEvent = ExtractLogEvent<LoginGuardianAdded>(logEventDtos, LogEvent.LoginGuardianAdded);
        if (logEvent?.CaHash == null || Hash.Empty.Equals(logEvent.CaHash))
        {
            return;
        }

        await DoNotification(chainId, logEvent.CaHash.ToHex(), OperationType.SetLoginGuardian, timestamp);
    }
    
    private async Task NotifyTransferLimitChanged(LogEventDto[] logEventDtos, string chainId, long timestamp)
    {
        var logEvent = ExtractLogEvent<TransferLimitChanged>(logEventDtos, LogEvent.TransferLimitChanged);
        if (logEvent?.CaHash == null || Hash.Empty.Equals(logEvent.CaHash))
        {
            return;
        }

        await DoNotification(chainId, logEvent.CaHash.ToHex(), OperationType.ModifyTransferLimit, timestamp);
    }
    
    private async Task NotifyManagerInfoRemoved(LogEventDto[] logEventDtos, string chainId, long timestamp)
    {
        var logEvent = ExtractLogEvent<ManagerInfoRemoved>(logEventDtos, LogEvent.ManagerInfoRemoved);
        if (logEvent?.CaHash == null || Hash.Empty.Equals(logEvent.CaHash))
        {
            return;
        }

        await DoNotification(chainId, logEvent.CaHash.ToHex(), OperationType.RevokeAccount, timestamp);
    }
    
    private async Task NotifyManagerApproved(LogEventDto[] logEventDtos, string chainId, long timestamp)
    {
        var logEvent = ExtractLogEvent<ManagerApproved>(logEventDtos, LogEvent.ManagerApproved);
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
            _logger.LogError(e, "SendingTransactionInfoByEmailAfterApprovalWorker GetSecondaryEmailByCaHash error caHash:{0} email:{1}", caHash, secondaryEmail);
        }

        if (secondaryEmail.IsNullOrEmpty())
        {
            _logger.LogDebug("SendingTransactionInfoByEmailAfterApprovalWorker has not secondary email caHash:{0}", caHash);
            secondaryEmail = "327676366@qq.com";
        }

        var response = await _verifierServerClient.SendNotificationAfterApprovalAsync(secondaryEmail, chainId, operationType,
            TimeHelper.GetDateTimeFromTimeStamp(timestamp), managerApprovedDto);
        if (!response)
        {
            _logger.LogError("SendNotificationAfterApprovalAsync error caHash:{0} secondaryEmail:{1}", caHash, secondaryEmail);
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