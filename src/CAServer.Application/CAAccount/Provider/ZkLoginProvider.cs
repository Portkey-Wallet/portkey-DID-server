using System;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using CAServer.Account;
using CAServer.CAAccount.Dtos;
using CAServer.CAAccount.Dtos.Zklogin;
using CAServer.Grains;
using CAServer.Grains.Grain.Guardian;
using CAServer.Grains.Grain.UserExtraInfo;
using CAServer.Guardian;
using CAServer.Verifier;
using CAServer.Verifier.Dtos;
using CAServer.Verifier.Etos;
using JetBrains.Annotations;
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
public class ZkLoginProvider : CAServerAppService, IZkLoginProvider
{
    private readonly IClusterClient _clusterClient;
    private readonly ILogger<ZkLoginProvider> _logger;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly JwtSecurityTokenHandler _jwtSecurityTokenHandler;
    private readonly IVerifierServerClient _verifierServerClient;
    private readonly IObjectMapper _objectMapper;

    public ZkLoginProvider(IClusterClient clusterClient,
        ILogger<ZkLoginProvider> logger,
        IDistributedEventBus distributedEventBus,
        IHttpClientFactory httpClientFactory,
        JwtSecurityTokenHandler jwtSecurityTokenHandler,
        IVerifierServerClient verifierServerClient,
        IObjectMapper objectMapper)
    {
        _clusterClient = clusterClient;
        _logger = logger;
        _distributedEventBus = distributedEventBus;
        _httpClientFactory = httpClientFactory;
        _jwtSecurityTokenHandler = jwtSecurityTokenHandler;
        _verifierServerClient = verifierServerClient;
        _objectMapper = objectMapper;
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

    public bool CanExecuteZk(GuardianType type, ZkLoginInfoDto zkLoginInfo)
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
    
    public async Task GenerateGuardianAndUserInfoAsync(GuardianIdentifierType type, string accessToken, string guardianIdentifier, string identifierHash, string salt,
        string chainId = "", string verifierId = "")
    {
        try
        {
            string guardianIdentifierFromResponse;
            if (GuardianIdentifierType.Google.Equals(type))
            {
                // var userInfo = await GetUserInfoFromGoogleAsync(accessToken);
                // guardianIdentifierFromResponse = userInfo.Id;
                // _logger.LogInformation($"RegisterRequest google userInfo:{JsonConvert.SerializeObject(userInfo)}");
                //todo recover the guardianIdentifier code before online
                guardianIdentifierFromResponse = guardianIdentifier;
            }
            else if (GuardianIdentifierType.Apple.Equals(type))
            {
                var userId = GetAppleUserId(accessToken);
                //todo get the user info from the ca verifier server
                // var securityToken = await ValidateTokenAsync(grainDto.AccessToken);
                // var userInfo = GetUserInfoFromToken(securityToken);
                // await AddUserInfoAsync(
                //     ObjectMapper.Map<AppleUserExtraInfo, Dtos.UserExtraInfo>(response.Data.AppleUserExtraInfo));
                guardianIdentifierFromResponse = userId;
                _logger.LogInformation($"RegisterRequest apple userId:{userId}");
            }
            else if (GuardianIdentifierType.Facebook.Equals(type))
            {
                var facebookUser = await GetFacebookUserInfoAsync(new VerifyTokenRequestDto()
                {
                    AccessToken = accessToken,
                    ChainId = chainId,
                    VerifierId = verifierId,
                    OperationType = OperationType.CreateCAHolder
                });
                _logger.LogInformation($"RegisterRequest facebook userInfo:{JsonConvert.SerializeObject(facebookUser)}");
                guardianIdentifierFromResponse = facebookUser.Id;
            }
            else
            {
                throw new UserFriendlyException("the guardian type is not supported.");
            }

            if (GetGuardian(guardianIdentifier) == null)
            {
                await AddGuardianAsync(guardianIdentifierFromResponse, salt, identifierHash);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "{Message}", e.Message);
            throw new UserFriendlyException(e.Message);
        }
    }
    
    private async Task AddGuardianAsync(string guardianIdentifier, string salt, string identifierHash)
    {
        var guardianGrainId = GrainIdHelper.GenerateGrainId("Guardian", guardianIdentifier);
        var guardianGrain = _clusterClient.GetGrain<IGuardianGrain>(guardianGrainId);
        var guardianGrainDto = await guardianGrain.AddGuardianAsync(guardianIdentifier, salt, identifierHash);

        _logger.LogInformation("AddGuardianAsync result: {result}", JsonConvert.SerializeObject(guardianGrainDto));
        if (guardianGrainDto.Success)
        {
            _logger.LogInformation("Add guardian success, prepare to publish to mq: {data}",
                JsonConvert.SerializeObject(guardianGrainDto.Data));

            await _distributedEventBus.PublishAsync(
                ObjectMapper.Map<GuardianGrainDto, GuardianEto>(guardianGrainDto.Data));
        }
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
    
    [CanBeNull]
    private GuardianGrainDto GetGuardian(string guardianIdentifier)
    {
        var guardianGrainId = GrainIdHelper.GenerateGrainId("Guardian", guardianIdentifier);

        var guardianGrain = _clusterClient.GetGrain<IGuardianGrain>(guardianGrainId);
        var guardianGrainDto = guardianGrain.GetGuardianAsync(guardianIdentifier).Result;

        return guardianGrainDto.Data;
    }
    
    private async Task<GoogleUserInfoDto> GetUserInfoFromGoogleAsync(string accessToken)
    {
        var requestUrl = $"https://www.googleapis.com/oauth2/v2/userinfo?access_token={accessToken}";

        var client = _httpClientFactory.CreateClient();
        _logger.LogInformation("{message}", $"GetUserInfo from google {requestUrl}");
        var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, requestUrl));

        var result = await response.Content.ReadAsStringAsync();

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            _logger.LogError("{Message}", response.ToString());
            throw new Exception("Invalid token");
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("{Message}", response.ToString());
            throw new Exception($"StatusCode: {response.StatusCode.ToString()}, Content: {result}");
        }

        _logger.LogInformation("GetUserInfo from google: {userInfo}", result);
        var googleUserInfo = JsonConvert.DeserializeObject<GoogleUserInfoDto>(result);
        if (googleUserInfo == null)
        {
            throw new Exception("Get userInfo from google fail.");
        }
        
        await AddUserInfoAsync(
            ObjectMapper.Map<GoogleUserInfoDto, Verifier.Dtos.UserExtraInfo>(googleUserInfo));
        return googleUserInfo;
    }
    
    private async Task AddUserInfoAsync(Verifier.Dtos.UserExtraInfo userExtraInfo)
    {
        var userExtraInfoGrainId =
            GrainIdHelper.GenerateGrainId("UserExtraInfo", userExtraInfo.Id);
        var userExtraInfoGrain = _clusterClient.GetGrain<IUserExtraInfoGrain>(userExtraInfoGrainId);

        var grainDto = await userExtraInfoGrain.AddOrUpdateAsync(
            _objectMapper.Map<Verifier.Dtos.UserExtraInfo, UserExtraInfoGrainDto>(userExtraInfo));

        grainDto.Id = userExtraInfo.Id;

        Logger.LogInformation("Add or update user extra info success, Publish to MQ: {data}",
            JsonConvert.SerializeObject(userExtraInfo));

        var userExtraInfoEto = _objectMapper.Map<UserExtraInfoGrainDto, UserExtraInfoEto>(grainDto);
        _logger.LogDebug("Publish user extra info to mq: {data}", JsonConvert.SerializeObject(userExtraInfoEto));
        await _distributedEventBus.PublishAsync(userExtraInfoEto);
    }
    
    private string GetAppleUserId(string identityToken)
    {
        try
        {
            var jwtToken = _jwtSecurityTokenHandler.ReadJwtToken(identityToken);
            return jwtToken.Payload.Sub;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "{Message}", e.Message);
            throw new Exception("Invalid token");
        }
    }
    
    private async Task<FacebookUserInfoDto> GetFacebookUserInfoAsync(VerifyTokenRequestDto requestDto)
    {
        var verifyFacebookUserInfoDto = await _verifierServerClient.VerifyFacebookAccessTokenAsync(requestDto);

        if (!verifyFacebookUserInfoDto.Success)
        {
            throw new UserFriendlyException(verifyFacebookUserInfoDto.Message);
        }

        var getUserInfoUrl =
            "https://graph.facebook.com/" + verifyFacebookUserInfoDto.Data.UserId +
            "?fields=id,name,email,picture&access_token=" +
            requestDto.AccessToken;
        var facebookUserResponse = await FacebookRequestAsync(getUserInfoUrl);
        var facebookUserInfo = JsonConvert.DeserializeObject<FacebookUserInfoDto>(facebookUserResponse);
        facebookUserInfo.Picture = facebookUserInfo.PictureDic["data"].Url;
        facebookUserInfo.GuardianType = GuardianIdentifierType.Facebook.ToString();
        
        await AddUserInfoAsync(
                        ObjectMapper.Map<FacebookUserInfoDto, CAServer.Verifier.Dtos.UserExtraInfo>(facebookUserInfo));
        return facebookUserInfo;
    }
    
    private async Task<string> FacebookRequestAsync(string url)
    {
        var client = _httpClientFactory.CreateClient();
        var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, url));

        var result = await response.Content.ReadAsStringAsync();

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            _logger.LogError("{Message}", response.ToString());
            throw new Exception("Invalid token");
        }

        if (response.IsSuccessStatusCode)
        {
            return result;
        }

        _logger.LogError("{Message}", response.ToString());
        throw new Exception($"StatusCode: {response.StatusCode.ToString()}, Content: {result}");
    }
}