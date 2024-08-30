using System;
using System.Threading.Tasks;
using CAServer.Common;
using CAServer.Grains.Grain.Contacts;
using CAServer.UserSecurity.Provider;
using CAServer.Verifier;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Orleans;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Caching;
using Volo.Abp.Identity;
using Volo.Abp.Threading;
using IContractProvider = CAServer.ContractEventHandler.Core.Application.IContractProvider;

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
    
    public SendingTransactionInfoByEmailAfterApprovalWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        IUserSecurityProvider userSecurityProvider,
        ILogger<SendingTransactionInfoByEmailAfterApprovalWorker> logger,
        IVerifierServerClient verifierServerClient,
        IClusterClient clusterClient,
        IdentityUserManager userManager,
        IContractProvider contractProvider,
        IDistributedCache<string> distributedCache,
        IHostApplicationLifetime hostApplicationLifetime,
        IBackgroundWorkerRegistrarProvider registrarProvider) : base(timer, serviceScopeFactory)
    {
        _registrarProvider = registrarProvider;
        _userSecurityProvider = userSecurityProvider;
        _logger = logger;
        _verifierServerClient = verifierServerClient;
        _clusterClient = clusterClient;
        _userManager = userManager;
        _contractProvider = contractProvider;
        _distributedCache = distributedCache;
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
            var response = await _verifierServerClient.SendNotificationAfterApprovalAsync(managerApprovedDto, secondaryEmail);
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