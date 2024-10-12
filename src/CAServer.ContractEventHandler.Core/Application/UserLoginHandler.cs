using System;
using System.Diagnostics;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using CAServer.Commons;
using CAServer.CryptoGift;
using CAServer.Etos;
using CAServer.Monitor.Interceptor;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
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
    
    public UserLoginHandler(IContractAppService contractAppService,
        ICryptoGiftAppService cryptoGiftAppService,
        IDistributedCache<string> distributedCache,
        ILogger<UserLoginHandler> logger)
    {
        _contractAppService = contractAppService;
        _cryptoGiftAppService = cryptoGiftAppService;
        _distributedCache = distributedCache;
        _logger = logger;
    }
    
    [ExceptionHandler(typeof(Exception),
        Message = "UserLoginHandler UserLoginEto exist error",  
        TargetType = typeof(ExceptionHandlingService), 
        MethodName = nameof(ExceptionHandlingService.HandleExceptionP1))
    ]
    public async Task HandleEventAsync(UserLoginEto eventData)
    {
        
        await CryptoGiftTransferRedPackageHandler(eventData);
        await _contractAppService.SyncOriginChainIdAsync(eventData);
    }

    private async Task CryptoGiftTransferRedPackageHandler(UserLoginEto eventData)
    {
        try
        {
            if (!eventData.FromCaServer.HasValue || !eventData.FromCaServer.Value)
            {
                _logger.LogInformation("UserLoginHandler userId:{0} caHash:{1} not from the caserver, ignored", eventData.UserId, eventData.CaHash);
                return;
            }
            _logger.LogInformation("userId:{0} time:{1} received login message:{2}", eventData.UserId, DateTimeOffset.Now.ToUnixTimeMilliseconds(), JsonConvert.SerializeObject(eventData));
            Stopwatch sw = new Stopwatch();
            sw.Start();
            var cachedUserId = await _distributedCache.GetAsync(string.Format(CryptoGiftConstant.UserLoginPrefix, eventData.CaHash));
            if (cachedUserId.IsNullOrEmpty())
            {
                await _distributedCache.SetAsync(string.Format(CryptoGiftConstant.UserLoginPrefix, eventData.CaHash), eventData.UserId.ToString());
            }
            var cacheResult = await _distributedCache.GetAsync(string.Format(CryptoGiftConstant.RegisterCachePrefix, eventData.CaHash));
            if (cacheResult.IsNullOrEmpty())
            {
                cacheResult = await _distributedCache.GetAsync(string.Format(CryptoGiftConstant.SocialRecoveryCachePrefix, eventData.CaHash));
                if (cacheResult.IsNullOrEmpty())
                {
                    return;
                }
            }
            var cryptoGiftReferralDto = JsonConvert.DeserializeObject<CryptoGiftReferralDto>(cacheResult);
            if (cryptoGiftReferralDto.ReferralInfo == null)
            {
                return;
            }
            await _cryptoGiftAppService.CryptoGiftTransferToRedPackage(eventData.UserId, eventData.CaHash, cryptoGiftReferralDto.CaAddress, cryptoGiftReferralDto.ReferralInfo, cryptoGiftReferralDto.IsNewUser, cryptoGiftReferralDto.IpAddress);
            sw.Stop();
            _logger.LogInformation("userId:{0} time:{1} cost:{2}ms", 
                eventData.UserId, DateTimeOffset.Now.ToUnixTimeMilliseconds(), sw.ElapsedMilliseconds);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "UserLoginHandler Handle Crypto Gift error");
        }
    }
}