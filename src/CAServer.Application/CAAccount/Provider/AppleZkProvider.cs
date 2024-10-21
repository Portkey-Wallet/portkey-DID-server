using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using CAServer.AppleVerify;
using CAServer.CAAccount.Dtos;
using CAServer.Common;
using CAServer.Verifier;
using CAServer.Verifier.Dtos;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.Caching;
using Volo.Abp.ObjectMapping;

namespace CAServer.CAAccount.Provider;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class AppleZkProvider : CAServerAppService, IAppleZkProvider
{
    private readonly IGuardianUserProvider _guardianUserProvider;
    private readonly ILogger<AppleZkProvider> _logger;
    private readonly JwtSecurityTokenHandler _jwtSecurityTokenHandler;
    private readonly IDistributedCache<AppleKeys, string> _distributedCache;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IVerifierServerClient _verifierServerClient;
    private readonly IGetVerifierServerProvider _verifierServerProvider;
    private readonly IObjectMapper _objectMapper;
    
    public AppleZkProvider(
        IGuardianUserProvider guardianUserProvider,
        ILogger<AppleZkProvider> logger,
        JwtSecurityTokenHandler jwtSecurityTokenHandler,
        IDistributedCache<AppleKeys, string> distributedCache,
        IHttpClientFactory httpClientFactory,
        IVerifierServerClient verifierServerClient,
        IGetVerifierServerProvider verifierServerProvider,
        IObjectMapper objectMapper)
    {
        _guardianUserProvider = guardianUserProvider;
        _logger = logger;
        _jwtSecurityTokenHandler = jwtSecurityTokenHandler;
        _distributedCache = distributedCache;
        _httpClientFactory = httpClientFactory;
        _verifierServerClient = verifierServerClient;
        _verifierServerProvider = verifierServerProvider;
        _objectMapper = objectMapper;
    }
    
    public async Task<string> SaveGuardianUserBeforeZkLoginAsync(VerifiedZkLoginRequestDto requestDto)
    {
        try
        {
            var userId = GetAppleUserId(requestDto.AccessToken);
            var hashInfo = await _guardianUserProvider.GetSaltAndHashAsync(userId, requestDto.Salt, requestDto.PoseidonIdentifierHash);
            var securityToken = await ValidateTokenAsync(requestDto.AccessToken);
            var userInfo = GetUserInfoFromToken(securityToken);
            userInfo.GuardianType = GuardianIdentifierType.Apple.ToString();
            userInfo.AuthTime = DateTime.UtcNow;
            SendNotification(requestDto, userInfo, hashInfo);
            if (!hashInfo.Item3)
            {
                await _guardianUserProvider.AddGuardianAsync(userId, hashInfo.Item2, hashInfo.Item1, requestDto.PoseidonIdentifierHash);
            }
            _logger.LogInformation("send Dtos.UserExtraInfo of Apple:{0}", JsonConvert.SerializeObject(userInfo));
            await _guardianUserProvider.AddUserInfoAsync(
                ObjectMapper.Map<AppleUserExtraInfo, CAServer.Verifier.Dtos.UserExtraInfo>(userInfo));

            return hashInfo.Item1;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "{Message}", e.Message);
            throw new UserFriendlyException(e.Message);
        }
    }

    private async void SendNotification(VerifiedZkLoginRequestDto requestDto, AppleUserExtraInfo userInfo, Tuple<string, string, bool> hashInfo)
    {
        if (requestDto.VerifierId.IsNullOrEmpty())
        {
            requestDto.VerifierId = await _verifierServerProvider.GetFirstVerifierServerEndPointAsync(requestDto.ChainId);
        }
        var verifyTokenRequestDto = _objectMapper.Map<VerifiedZkLoginRequestDto, VerifyTokenRequestDto>(requestDto);
        var guardianIdentifier = userInfo.Email.IsNullOrEmpty() ? string.Empty : userInfo.Email;
        await _guardianUserProvider.AppendSecondaryEmailInfo(verifyTokenRequestDto, hashInfo.Item1, guardianIdentifier, GuardianIdentifierType.Apple);
        var response =
            await _verifierServerClient.VerifyAppleTokenAsync(verifyTokenRequestDto, hashInfo.Item1, hashInfo.Item2);
        if (!response.Success)
        {
            _logger.LogError($"Validate VerifierApple Failed :{response.Message}");
        }
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

    private async Task<SecurityToken> ValidateTokenAsync(string identityToken)
    {
        try
        {
            var jwtToken = _jwtSecurityTokenHandler.ReadJwtToken(identityToken);
            var kid = jwtToken.Header["kid"].ToString();
            var appleKey = await GetAppleKeyAsync(kid);
            var jwk = new JsonWebKey(JsonConvert.SerializeObject(appleKey));

            var validateParameter = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = "https://appleid.apple.com",
                ValidateAudience = true,
                //_appleAuthOptions.Audiences, in appsettings.json, not in appolo
                ValidAudiences = new List<string>{
                    "com.portkey.finance",
                    "com.portkey.did",
                    "did.portkey",
                    "com.portkey.did.tran",
                    "com.portkey.did.extension.service"
                },
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = jwk
            };

            _jwtSecurityTokenHandler.ValidateToken(identityToken, validateParameter,
                out SecurityToken validatedToken);

            return validatedToken;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Get UserInfo From Apple Failed:" + e.Message);
            throw new UserFriendlyException("Get UserInfo From Apple Failed");
        }
    }
    
    private async Task<AppleKey> GetAppleKeyAsync(string kid)
    {
        var appleKeys = await GetAppleKeysAsync();
        return appleKeys.Keys.FirstOrDefault(t => t.Kid == kid);
    }

    private async Task<AppleKeys> GetAppleKeysAsync()
    {
        return await _distributedCache.GetOrAddAsync(
            "apple.auth.keys",
            async () => await GetAppleKeyFormAppleAsync(),
            () => new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = DateTimeOffset.Now.AddHours(1/*_appleAuthOptions.KeysExpireTime*/) //todo
            }
        );
    }

    private async Task<AppleKeys> GetAppleKeyFormAppleAsync()
    {
        var appleKeyUrl = "https://appleid.apple.com/auth/keys";
        var response = await _httpClientFactory.CreateClient().GetStringAsync(appleKeyUrl);

        return JsonConvert.DeserializeObject<AppleKeys>(response);
    }
    
    private static AppleUserExtraInfo GetUserInfoFromToken(SecurityToken validatedToken)
    {
        var jwtPayload = ((JwtSecurityToken)validatedToken).Payload;
        var userInfo = new AppleUserExtraInfo
        {
            Id = jwtPayload.Sub
        };

        if (jwtPayload.TryGetValue("email", out var email))
        {
            userInfo.Email = email.ToString();
        }

        if (jwtPayload.TryGetValue("email_verified", out var verifiedEmail))
        {
            userInfo.VerifiedEmail = Convert.ToBoolean(verifiedEmail);
        }

        if (jwtPayload.TryGetValue("is_private_email", out var privateEmail))
        {
            userInfo.IsPrivateEmail = Convert.ToBoolean(privateEmail);
        }

        return userInfo;
    }

    public async Task<AppleUserExtraInfo> GetAppleUserExtraInfo(string accessToken)
    {
        SecurityToken securityToken;
        try
        {
            securityToken = await ValidateTokenAsync(accessToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetAppleUserExtraInfo ValidateTokenAsync failed");
            return null;
        }
        return GetUserInfoFromToken(securityToken);
    }
}