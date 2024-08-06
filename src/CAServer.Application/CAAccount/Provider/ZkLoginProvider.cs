using System;
using System.Threading.Tasks;
using CAServer.Account;
using CAServer.CAAccount.Dtos;
using CAServer.CAAccount.Dtos.Zklogin;
using CAServer.Grains;
using CAServer.Grains.Grain.Guardian;
using CAServer.Guardian;
using CAServer.Verifier;
using CAServer.Verifier.Dtos;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Orleans;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace CAServer.CAAccount.Provider;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class ZkLoginProvider
    : CAServerAppService, IZkLoginProvider
{
    private readonly IClusterClient _clusterClient;
    private readonly ILogger<ZkLoginProvider> _logger;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly IObjectMapper _objectMapper;
    private readonly IGoogleZkProvider _googleZkProvider;
    private readonly IAppleZkProvider _appleZkProvider;
    private readonly IFacebookZkProvider _facebookZkProvider;
    public ZkLoginProvider(
        IClusterClient clusterClient,
        ILogger<ZkLoginProvider> logger,
        IDistributedEventBus distributedEventBus,
        IObjectMapper objectMapper,
        IGoogleZkProvider googleZkProvider,
        IAppleZkProvider appleZkProvider,
        IFacebookZkProvider facebookZkProvider)
    {
        _clusterClient = clusterClient;
        _logger = logger;
        _distributedEventBus = distributedEventBus;
        _objectMapper = objectMapper;
        _googleZkProvider = googleZkProvider;
        _appleZkProvider = appleZkProvider;
        _facebookZkProvider = facebookZkProvider;
    }
    public bool CanSupportZk(GuardianIdentifierType type)
    {
        return GuardianIdentifierType.Google.Equals(type)
               || GuardianIdentifierType.Apple.Equals(type)
               || GuardianIdentifierType.Facebook.Equals(type);
    }

    public bool CanExecuteZk(GuardianIdentifierType type, ZkLoginInfoRequestDto zkLoginInfo)
    {
        if (!CanSupportZk(type))
        {
            return false;
        }

        return zkLoginInfo != null
               && zkLoginInfo.IdentifierHash is not (null or "")
               && zkLoginInfo.Salt is not (null or "")
               && zkLoginInfo.Jwt is not (null or "")
               && zkLoginInfo.Nonce is not (null or "")
               && zkLoginInfo.ZkProof is not (null or "")
               && zkLoginInfo.CircuitId is not (null or "")
               /*&& zkLoginInfo.Timestamp > 0*/;
    }
    
    private bool CanSupportZk(GuardianType type)
    {
        return GuardianType.GUARDIAN_TYPE_OF_GOOGLE.Equals(type)
               || GuardianType.GUARDIAN_TYPE_OF_APPLE.Equals(type)
               || GuardianType.GUARDIAN_TYPE_OF_FACEBOOK.Equals(type);
    }

    public bool CanExecuteZkByZkLoginInfoDto(GuardianType type, ZkLoginInfoDto zkLoginInfo)
    {
        if (!CanSupportZk(type))
        {
            return false;
        }
        return zkLoginInfo is not null
               && zkLoginInfo.IdentifierHash is not (null or "")
               && zkLoginInfo.Salt is not (null or "")
               && zkLoginInfo.Nonce is not (null or "")
               && zkLoginInfo.ZkProof is not (null or "")
               && zkLoginInfo.CircuitId is not (null or "")
               && zkLoginInfo.Issuer is not (null or "")
               && zkLoginInfo.Kid is not (null or "")
               && zkLoginInfo.NoncePayload is not null;
    }
    
    public async Task<GuardianEto> UpdateGuardianAsync(string guardianIdentifier, string salt, string identifierHash)
    {
        var guardianGrainId = GrainIdHelper.GenerateGrainId("Guardian", guardianIdentifier);
        var guardianGrain = _clusterClient.GetGrain<IGuardianGrain>(guardianGrainId);
        var guardianGrainDto = await guardianGrain.UpdateGuardianAsync(guardianIdentifier, salt, identifierHash);
        _logger.LogInformation("UpdateGuardianAsync result: {result}", JsonConvert.SerializeObject(guardianGrainDto));
        var eto = _objectMapper.Map<GuardianGrainDto, GuardianEto>(guardianGrainDto.Data);
        if (guardianGrainDto.Success)
        {
            await _distributedEventBus.PublishAsync(eto);
        }
        return eto;
    }
    
    public async Task<VerifiedZkResponse> VerifiedZkLoginAsync(VerifiedZkLoginRequestDto requestDto)
    {
        var verifyTokenRequestDto = _objectMapper.Map<VerifiedZkLoginRequestDto, VerifyTokenRequestDto>(requestDto);
        string identifierHash = null;
        if (GuardianIdentifierType.Google.Equals(requestDto.Type))
        {
            try
            {
                identifierHash = await _googleZkProvider.SaveGuardianUserBeforeZkLoginAsync(requestDto);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Google SaveGuardianUserBeforeZkLogin error");
                throw new UserFriendlyException("add google guardian and user extra info error");
            }
        }
        if (GuardianIdentifierType.Apple.Equals(requestDto.Type))
        {
            try
            {
                identifierHash = await _appleZkProvider.SaveGuardianUserBeforeZkLoginAsync(requestDto);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Apple SaveGuardianUserBeforeZkLogin error");
                throw new UserFriendlyException("add apple guardian and user extra info error");
            }
        }
        if (GuardianIdentifierType.Facebook.Equals(requestDto.Type))
        {
            try
            {
                identifierHash = await _facebookZkProvider.SaveGuardianUserBeforeZkLoginAsync(requestDto);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Facebook SaveGuardianUserBeforeZkLogin error");
                throw new UserFriendlyException("add facebook guardian and user extra info error");
            }
        }

        return new VerifiedZkResponse()
        {
            GuardianIdentifierHash = identifierHash
        };
    }

    public async Task TriggerZkLoginWorker(ZkEto caHashes)
    {
        await _distributedEventBus.PublishAsync(caHashes);
    }
}