using System;
using System.Threading.Tasks;
using CAServer.Commons;
using CAServer.CryptoGift;
using CAServer.Etos;
using CAServer.Grains.Grain.Contacts;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Orleans;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace CAServer.ContractEventHandler.Core.Application;

public class UserLoginHandler : IDistributedEventHandler<UserLoginEto>,ITransientDependency
{
    private readonly IContractAppService _contractAppService;
    private readonly ICryptoGiftAppService _cryptoGiftAppService;
    private readonly IDistributedCache<string> _distributedCache;
    private readonly ILogger<UserLoginHandler> _logger;
    private readonly IClusterClient _clusterClient;
    
    public UserLoginHandler(IContractAppService contractAppService,
        ICryptoGiftAppService cryptoGiftAppService,
        IDistributedCache<string> distributedCache,
        ILogger<UserLoginHandler> logger,
        IClusterClient clusterClient)
    {
        _contractAppService = contractAppService;
        _cryptoGiftAppService = cryptoGiftAppService;
        _distributedCache = distributedCache;
        _logger = logger;
        _clusterClient = clusterClient;
    }
    
    public async Task HandleEventAsync(UserLoginEto eventData)
    {
        await CryptoGiftTransferRedPackageHandler(eventData);
        
        try
        {
            await _contractAppService.SyncOriginChainIdAsync(eventData);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "UserLoginHandler HandleEventAsync error");
        }
    }

    private async Task CryptoGiftTransferRedPackageHandler(UserLoginEto eventData)
    {
        try
        {
            _logger.LogInformation("UserLoginHandler receive message:{0}", JsonConvert.SerializeObject(eventData));
            
            var cachedUserId = await _distributedCache.GetAsync($"UserLoginHandler:{eventData.CaHash}");
            if (cachedUserId.IsNullOrEmpty())
            {
                await _distributedCache.SetAsync($"UserLoginHandler:{eventData.CaHash}", eventData.UserId.ToString());
            }
            var cacheResult = await _distributedCache.GetAsync(string.Format(CryptoGiftConstant.RegisterCachePrefix, eventData.CaHash));
            if (cacheResult.IsNullOrEmpty())
            {
                _logger.LogInformation("UserLoginHandler register cacheResult is null, eventData:{0}", eventData);
                cacheResult = await _distributedCache.GetAsync(string.Format(CryptoGiftConstant.SocialRecoveryCachePrefix, eventData.CaHash));
                if (cacheResult.IsNullOrEmpty())
                {
                    _logger.LogInformation("UserLoginHandler social recovery cacheResult is null, eventData:{0}", eventData);
                    return;
                }
            }
            _logger.LogInformation("UserLoginHandler caHash:{0} cacheResult:{1}", eventData.CaHash, cacheResult);
            var cryptoGiftReferralDto = JsonConvert.DeserializeObject<CryptoGiftReferralDto>(cacheResult);
            _logger.LogInformation("UserLoginHandler caHash:{0} cryptoGiftReferralDto:{1}", eventData.CaHash, JsonConvert.SerializeObject(cryptoGiftReferralDto));
            _logger.LogInformation($"CryptoGiftTransferToRedPackage userId:{eventData.UserId},caAddress:{cryptoGiftReferralDto.CaAddress},referralInfo:{cryptoGiftReferralDto.ReferralInfo}," +
                                   $"isNewUser:{cryptoGiftReferralDto.IsNewUser},ipAddress:{cryptoGiftReferralDto.IpAddress}");
            if (cryptoGiftReferralDto.ReferralInfo == null)
            {
                return;
            }
            //todo 用户的新注册标注位在这里更新合理，只有站外红包业务需要用到，然后再登录态抢红包和登录详情页使用
            var grain = _clusterClient.GetGrain<ICAHolderGrain>(eventData.UserId);
            var caHolderGrainDto = await grain.UpdateNewUserMarkAsync(cryptoGiftReferralDto.IsNewUser);
            _logger.LogInformation("UserLoginHandler update caHolderGrainDto:{0}", JsonConvert.SerializeObject(caHolderGrainDto.Data));
            await _cryptoGiftAppService.CryptoGiftTransferToRedPackage(eventData.UserId, cryptoGiftReferralDto.CaAddress, cryptoGiftReferralDto.ReferralInfo, cryptoGiftReferralDto.IsNewUser, cryptoGiftReferralDto.IpAddress);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "UserLoginHandler Handle Crypto Gift error");
        }
    }
}