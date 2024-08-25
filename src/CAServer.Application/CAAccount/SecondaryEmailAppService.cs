using System;
using System.Threading.Tasks;
using CAServer.CAAccount.Cmd;
using CAServer.CAAccount.Dtos;
using CAServer.Commons;
using CAServer.Etos;
using CAServer.Grains.Grain.Contacts;
using CAServer.Transfer.Dtos;
using CAServer.Verifier;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
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

    public SecondaryEmailAppService(IDistributedCache<SecondaryEmailCacheDto> distributedCache,
        ILogger<SecondaryEmailAppService> logger,
        IVerifierServerClient verifierServerClient,
        IClusterClient clusterClient,
        IDistributedEventBus distributedEventBus)
    {
        _distributedCache = distributedCache;
        _logger = logger;
        _verifierServerClient = verifierServerClient;
        _clusterClient = clusterClient;
        _distributedEventBus = distributedEventBus;
    }
    
    public async Task<ResponseWrapDto<VerifySecondaryEmailResponse>> VerifySecondaryEmailAsync(VerifySecondaryEmailCmd cmd)
    {
        var verifierSessionId = new Guid().ToString();
        var result = await _verifierServerClient.SendSecondaryEmailVerificationRequestAsync(cmd.SecondaryEmail, verifierSessionId);
        if (!result.Success || result.Data == null)
        {
            return new ResponseWrapDto<VerifySecondaryEmailResponse>
            {
                Code = ((int)ResponseCode.BusinessError).ToString(),
                Message = result.Message
            };
        }

        var key = GetCacheKey(CurrentUser.Id.ToString(), result.Data.VerifierSessionId.ToString());
        var value = new SecondaryEmailCacheDto()
        {
            SecondaryEmail = cmd.SecondaryEmail,
            VerifierServerEndpoint = result.Data.VerifierServerEndpoint
        };
        await _distributedCache.SetAsync(key, value, new DistributedCacheEntryOptions()
        {
            //todo convert to cache
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(600)
        });
        return new ResponseWrapDto<VerifySecondaryEmailResponse>
        {
            Code = ((int)ResponseCode.Succeed).ToString(),
            Message = ResponseCode.Succeed.ToString(),
            Data = new VerifySecondaryEmailResponse()
            {
                VerifierSessionId = result.Data.VerifierSessionId.ToString()
            }
        };
    }

    private static string GetCacheKey(string userId, string sessionId)
    {
        return string.Format(CachePrefix, userId, sessionId);
    }

    public async Task<ResponseWrapDto<VerifySecondaryEmailCodeResponse>> VerifySecondaryEmailCodeAsync(VerifySecondaryEmailCodeCmd cmd)
    {
        var (key, secondaryEmailCache) = await CheckCacheAndGetEmail(cmd.VerifierSessionId);
        if (secondaryEmailCache == null || secondaryEmailCache.SecondaryEmail.IsNullOrEmpty())
        {
            return new ResponseWrapDto<VerifySecondaryEmailCodeResponse>()
            {
                Code = ((int)ResponseCode.SessionTimeout).ToString(),
                Message = ResponseCode.SessionTimeout.ToString(),
            };
        }

        var result = await _verifierServerClient.VerifySecondaryEmailCodeAsync(
            cmd.VerifierSessionId, cmd.VerificationCode, secondaryEmailCache.VerifierServerEndpoint);
        if (!result.Success || result.Data == null)
        {
            return new ResponseWrapDto<VerifySecondaryEmailCodeResponse>
            {
                Code = ((int)ResponseCode.BusinessError).ToString(),
                Message = result.Message,
                Data = new VerifySecondaryEmailCodeResponse()
                {
                    VerifiedResult = false
                }
            };
        }
        return new ResponseWrapDto<VerifySecondaryEmailCodeResponse>
        {
            Code = ((int)ResponseCode.Succeed).ToString(),
            Message = ResponseCode.Succeed.ToString(),
            Data = new VerifySecondaryEmailCodeResponse()
            {
                VerifiedResult = true
            }
        };
    }

    public async Task<ResponseWrapDto<SetSecondaryEmailResponse>> SetSecondaryEmailAsync(SetSecondaryEmailCmd cmd)
    {
        var (key, secondaryEmailCache) = await CheckCacheAndGetEmail(cmd.VerifierSessionId);
        if (secondaryEmailCache == null || secondaryEmailCache.SecondaryEmail.IsNullOrEmpty())
        {
            return new ResponseWrapDto<SetSecondaryEmailResponse>()
            {
                Code = ((int)ResponseCode.SessionTimeout).ToString(),
                Message = ResponseCode.SessionTimeout.ToString(),
            };
        }
        
        var grain = _clusterClient.GetGrain<ICAHolderGrain>(CurrentUser.GetId());

        var result = await grain.AppendOrUpdateSecondaryEmailAsync(secondaryEmailCache.SecondaryEmail);
        if (!result.Success || result.Data == null)
        {
            return new ResponseWrapDto<SetSecondaryEmailResponse>()
            {
                Code = ((int)ResponseCode.BusinessError).ToString(),
                Message = result.Message,
                Data = new SetSecondaryEmailResponse()
                {
                    SetResult = false
                }
            };
        }
        //保存成功以后，发布邮箱保存或者更新的事件，事件中主要涉及cahash和secondaryEmail
        await _distributedEventBus.PublishAsync(new AccountEmailEto()
        {
            CaHash = result.Data.CaHash,
            SecondaryEmail = result.Data.SecondaryEmail
        });
        //1、消息接收者通过扫链GraphQlHelper通过cahash查询GuardianList
        //2、GuardianList通过Guardian的IdentifierHash查询es
        //3、es的Guardian数据新增或者更新cahash和secondaryEmail字段
        //4、verifyGoogleToken接口，已有Guardian的通过Guardian的IdentifierHash查询es中的cahash和secondaryEmail发送交易前的通知邮件
        //   新增的Guardian让前端补个loginGuardianIdentifierHash或者cahash参数
        await _distributedCache.RemoveAsync(key);
        return new ResponseWrapDto<SetSecondaryEmailResponse>()
        {
            Code = ((int)ResponseCode.Succeed).ToString(),
            Message = ResponseCode.Succeed.ToString(),
            Data = new SetSecondaryEmailResponse()
            {
                SetResult = true
            }
        };
    }

    private async Task<(string key, SecondaryEmailCacheDto secondaryEmailCache)> CheckCacheAndGetEmail(string verifierSessionId)
    {
        var key = GetCacheKey(CurrentUser.Id.ToString(), verifierSessionId);
        var secondaryEmailCache = await _distributedCache.GetAsync(key);
        return (key, secondaryEmailCache);
    }
}