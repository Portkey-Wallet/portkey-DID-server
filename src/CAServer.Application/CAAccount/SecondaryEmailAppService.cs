using System;
using System.Threading.Tasks;
using CAServer.CAAccount.Cmd;
using CAServer.CAAccount.Dtos;
using CAServer.CAAccount.Provider;
using CAServer.Commons;
using CAServer.Etos;
using CAServer.Grains.Grain.Contacts;
using CAServer.Verifier;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Orleans;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.Caching;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.Users;

namespace CAServer.CAAccount;

[RemoteService(false)]
[DisableAuditing]
public class SecondaryEmailAppService : CAServerAppService, ISecondaryEmailAppService
{
    private const string CachePrefix = "Portkey:SecondaryEmail:{0}:{1}";
    private readonly IDistributedCache<SecondaryEmailCacheDto> _distributedCache;
    private readonly ILogger<SecondaryEmailAppService> _logger;
    private readonly IVerifierServerClient _verifierServerClient;
    private readonly IClusterClient _clusterClient;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly SecondaryEmailOptions _secondaryEmailOptions;

    public SecondaryEmailAppService(IDistributedCache<SecondaryEmailCacheDto> distributedCache,
        ILogger<SecondaryEmailAppService> logger,
        IVerifierServerClient verifierServerClient,
        IClusterClient clusterClient,
        IDistributedEventBus distributedEventBus,
        IOptions<SecondaryEmailOptions> secondaryEmailOptions)
    {
        _distributedCache = distributedCache;
        _logger = logger;
        _verifierServerClient = verifierServerClient;
        _clusterClient = clusterClient;
        _distributedEventBus = distributedEventBus;
        _secondaryEmailOptions = secondaryEmailOptions.Value;
    }
    
    public async Task<VerifySecondaryEmailResponse> VerifySecondaryEmailAsync(VerifySecondaryEmailCmd cmd)
    {
        var verifierSessionId = Guid.NewGuid().ToString();
        var result = await _verifierServerClient.SendSecondaryEmailVerificationRequestAsync(cmd.SecondaryEmail, verifierSessionId);
        if (!result.Success || result.Data == null)
        {
            throw new UserFriendlyException(result.Message);
        }

        var key = GetCacheKey(CurrentUser.Id.ToString(), result.Data.VerifierSessionId.ToString());
        var value = new SecondaryEmailCacheDto()
        {
            SecondaryEmail = cmd.SecondaryEmail,
            VerifierServerEndpoint = result.Data.VerifierServerEndpoint
        };
        await _distributedCache.SetAsync(key, value, new DistributedCacheEntryOptions()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(_secondaryEmailOptions.CacheSeconds)
        });
        return  new VerifySecondaryEmailResponse()
        {
            VerifierSessionId = result.Data.VerifierSessionId.ToString()
        };
    }

    private static string GetCacheKey(string userId, string sessionId)
    {
        return string.Format(CachePrefix, userId, sessionId);
    }

    public async Task<VerifySecondaryEmailCodeResponse> VerifySecondaryEmailCodeAsync(VerifySecondaryEmailCodeCmd cmd)
    {
        var (key, secondaryEmailCache) = await CheckCacheAndGetEmail(cmd.VerifierSessionId);
        if (secondaryEmailCache == null || secondaryEmailCache.SecondaryEmail.IsNullOrEmpty())
        {
            throw new UserFriendlyException(CommonConstant.VerificationCodeExpired);
        }
        
        var result = await _verifierServerClient.VerifySecondaryEmailCodeAsync(
            cmd.VerifierSessionId, cmd.VerificationCode, secondaryEmailCache.SecondaryEmail, secondaryEmailCache.VerifierServerEndpoint);
        if (!result.Success)
        {
            throw new UserFriendlyException(result.Message);
        }

        var saveResult = await SetSecondaryEmailAsync(new SetSecondaryEmailCmd()
        {
            VerifierSessionId = cmd.VerifierSessionId
        });
        return new VerifySecondaryEmailCodeResponse()
        {
            VerifiedResult = saveResult is { SetResult: true }
        };
    }

    public async Task<SetSecondaryEmailResponse> SetSecondaryEmailAsync(SetSecondaryEmailCmd cmd)
    {
        var (key, secondaryEmailCache) = await CheckCacheAndGetEmail(cmd.VerifierSessionId);
        if (secondaryEmailCache == null || secondaryEmailCache.SecondaryEmail.IsNullOrEmpty())
        {
            throw new UserFriendlyException(CommonConstant.VerificationCodeExpired);
        }
        
        var grain = _clusterClient.GetGrain<ICAHolderGrain>(CurrentUser.GetId());
        var result = await grain.AppendOrUpdateSecondaryEmailAsync(secondaryEmailCache.SecondaryEmail);
        if (!result.Success || result.Data == null)
        {
            return new SetSecondaryEmailResponse()
            {
                SetResult = false
            };
        }
        await _distributedEventBus.PublishAsync(new AccountEmailEto()
        {
            CaHash = result.Data.CaHash,
            SecondaryEmail = result.Data.SecondaryEmail
        });
        await _distributedCache.RemoveAsync(key);
        return new SetSecondaryEmailResponse()
        {
            SetResult = true
        };
    }

    private async Task<(string key, SecondaryEmailCacheDto secondaryEmailCache)> CheckCacheAndGetEmail(string verifierSessionId)
    {
        var key = GetCacheKey(CurrentUser.Id.ToString(), verifierSessionId);
        var secondaryEmailCache = await _distributedCache.GetAsync(key);
        return (key, secondaryEmailCache);
    }

    public async Task<GetSecondaryEmailResponse> GetSecondaryEmailAsync(Guid userId)
    {
        var grain = _clusterClient.GetGrain<ICAHolderGrain>(userId);
        var caHolderResult = await grain.GetCaHolder();
        if (!caHolderResult.Success || caHolderResult.Data == null)
        {
            throw new UserFriendlyException(caHolderResult.Message);
        };
        
        return new GetSecondaryEmailResponse()
        {
            SecondaryEmail = caHolderResult.Data.SecondaryEmail
        };
    }
}