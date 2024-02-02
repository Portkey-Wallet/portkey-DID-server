using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading.Tasks;
using CAServer.AppleAuth.Dtos;
using CAServer.AppleAuth.Provider;
using CAServer.AppleVerify;
using CAServer.CAAccount.Dtos;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Grains;
using CAServer.Grains.Grain.UserExtraInfo;
using CAServer.Options;
using CAServer.SecurityServer;
using CAServer.Signature.Provider;
using CAServer.Verifier.Etos;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Orleans;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.EventBus.Distributed;

namespace CAServer.AppleAuth;

[RemoteService(false), DisableAuditing]
public class AppleAuthAppService : CAServerAppService, IAppleAuthAppService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AppleAuthOptions _appleAuthOptions;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly IClusterClient _clusterClient;
    private readonly IHttpClientService _httpClientService;
    private readonly JwtSecurityTokenHandler _jwtSecurityTokenHandler;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IAppleUserProvider _appleUserProvider;
    private readonly AppleTransferOptions _appleTransferOptions;
    private readonly ISecretProvider _secretProvider;

    public AppleAuthAppService(IHttpClientFactory httpClientFactory,
        IOptions<AppleAuthOptions> appleAuthVerifyOption,
        IDistributedEventBus distributedEventBus,
        IClusterClient clusterClient,
        IHttpClientService httpClientService,
        JwtSecurityTokenHandler jwtSecurityTokenHandler,
        IHttpContextAccessor httpContextAccessor,
        IAppleUserProvider appleUserProvider,
        IOptionsSnapshot<AppleTransferOptions> appleTransferOptions, 
        ISecretProvider secretProvider)
    {
        _httpClientFactory = httpClientFactory;
        _appleAuthOptions = appleAuthVerifyOption.Value;
        _clusterClient = clusterClient;
        _distributedEventBus = distributedEventBus;
        _httpClientService = httpClientService;
        _jwtSecurityTokenHandler = jwtSecurityTokenHandler;
        _httpContextAccessor = httpContextAccessor;
        _appleUserProvider = appleUserProvider;
        _secretProvider = secretProvider;
        _appleTransferOptions = appleTransferOptions.Value;
    }

    public async Task ReceiveAsync(AppleAuthDto appleAuthDto)
    {
        Logger.LogInformation("Apple token: {token}", JsonConvert.SerializeObject(appleAuthDto));

        var identityToken = appleAuthDto.Id_token;
        if (string.IsNullOrEmpty(appleAuthDto.Id_token))
        {
            if (_appleTransferOptions.CloseLogin)
            {
                throw new UserFriendlyException(_appleTransferOptions.ErrorMessage);
            }

            identityToken = await GetTokenAsync(appleAuthDto.Code);
        }

        var securityToken = await ValidateTokenAsync(identityToken);
        var jwtPayload = ((JwtSecurityToken)securityToken).Payload;

        if (_appleTransferOptions.IsNeedIntercept(jwtPayload.Sub))
        {
            throw new UserFriendlyException(_appleTransferOptions.ErrorMessage);
        }

        if (string.IsNullOrWhiteSpace(appleAuthDto.User))
        {
            await CheckUserAsync(jwtPayload.Sub);
            return;
        }

        var userExtraInfo = JsonConvert.DeserializeObject<AppleExtraInfo>(appleAuthDto.User);
        if (userExtraInfo == null)
        {
            throw new Exception("Failed to resolve user");
        }

        var userInfo = new Verifier.Dtos.UserExtraInfo
        {
            Id = jwtPayload.Sub,
            FirstName = userExtraInfo.Name.FirstName,
            LastName = userExtraInfo.Name.LastName,
            Email = userExtraInfo.Email,
            GuardianType = GuardianIdentifierType.Apple.ToString(),
            AuthTime = DateTime.UtcNow
        };

        await _appleUserProvider.SetUserExtraInfoAsync(new AppleUserExtraInfo
        {
            UserId = userInfo.Id,
            FirstName = userInfo.FirstName,
            LastName = userInfo.LastName,
        });

        if (jwtPayload.ContainsKey("email_verified"))
        {
            userInfo.VerifiedEmail = Convert.ToBoolean(jwtPayload["email_verified"]);
        }

        if (jwtPayload.ContainsKey("is_private_email"))
        {
            userInfo.IsPrivateEmail = Convert.ToBoolean(jwtPayload["is_private_email"]);
        }

        await AddUserInfoAsync(userInfo);
    }

    private async Task CheckUserAsync(string userId)
    {
        try
        {
            var exist = await _appleUserProvider.UserExtraInfoExistAsync(userId);
            if (exist) return;

            var extraInfo = await _appleUserProvider.GetUserInfoAsync(userId);
            if (extraInfo == null)
            {
                Logger.LogInformation("user not exist.");
                return;
            }

            var userExtraInfo = new Verifier.Dtos.UserExtraInfo
            {
                Id = userId,
                GuardianType = GuardianIdentifierType.Apple.ToString(),
                AuthTime = DateTime.UtcNow,
                FirstName = extraInfo.FirstName,
                LastName = extraInfo.LastName,
                Email = extraInfo.Email,
                IsPrivateEmail = extraInfo.IsPrivate,
                VerifiedEmail = extraInfo.VerifiedEmail,
                Picture = extraInfo.Picture
            };

            await _appleUserProvider.SetUserExtraInfoAsync(new AppleUserExtraInfo
            {
                UserId = userExtraInfo.Id,
                FirstName = userExtraInfo.FirstName,
                LastName = userExtraInfo.LastName,
            });
            await AddUserInfoAsync(userExtraInfo);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "check apple user exist error, userId:{userId}", userId);
        }
    }

    private async Task AddUserInfoAsync(Verifier.Dtos.UserExtraInfo userExtraInfo)
    {
        var userExtraInfoGrainId =
            GrainIdHelper.GenerateGrainId("UserExtraInfo", userExtraInfo.Id);

        var userExtraInfoGrain = _clusterClient.GetGrain<IUserExtraInfoGrain>(userExtraInfoGrainId);

        var grainDto = await userExtraInfoGrain.AddOrUpdateAppleUserAsync(
            ObjectMapper.Map<Verifier.Dtos.UserExtraInfo, UserExtraInfoGrainDto>(userExtraInfo));

        grainDto.Id = userExtraInfo.Id;
        await _distributedEventBus.PublishAsync(
            ObjectMapper.Map<UserExtraInfoGrainDto, UserExtraInfoEto>(grainDto), false, false);
    }

    private async Task<SecurityToken> ValidateTokenAsync(string identityToken)
    {
        try
        {
            var jwtToken = _jwtSecurityTokenHandler.ReadJwtToken(identityToken);
            var kid = jwtToken.Header["kid"].ToString();

            var appleKeys = await GetAppleKeyFormAppleAsync();
            var jwk = new JsonWebKey(
                JsonConvert.SerializeObject(appleKeys.Keys.FirstOrDefault(t => t.Kid == kid)));

            var validateParameter = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = "https://appleid.apple.com",
                ValidateAudience = true,
                ValidAudiences = _appleAuthOptions.Audiences,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = jwk
            };

            _jwtSecurityTokenHandler.ValidateToken(identityToken, validateParameter,
                out SecurityToken validatedToken);

            return validatedToken;
        }
        catch (SecurityTokenExpiredException e)
        {
            Logger.LogError(e, "The token has expired, {token}", identityToken);
            throw new UserFriendlyException("The token has expired.");
        }
        catch (SecurityTokenException e)
        {
            Logger.LogError(e, "Valid token fail, {token}", identityToken);
            throw new UserFriendlyException("Valid token fail.");
        }
        catch (ArgumentException e)
        {
            Logger.LogError(e, "Invalid token, {token}", identityToken);
            throw new UserFriendlyException("Invalid token.");
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Valid token error, {token}", identityToken);
            throw new UserFriendlyException(e.Message);
        }
    }

    private async Task<AppleKeys> GetAppleKeyFormAppleAsync()
    {
        var appleKeyUrl = "https://appleid.apple.com/auth/keys";
        return await _httpClientService.GetAsync<AppleKeys>(appleKeyUrl);
    }

    private async Task<string> GetTokenAsync(string code)
    {
        var url = "https://appleid.apple.com/auth/token";
        
        var secret = await _secretProvider.GetAppleAuthSignatureAsync(_appleAuthOptions.ExtensionConfig.KeyId,
            _appleAuthOptions.ExtensionConfig.TeamId, _appleAuthOptions.ExtensionConfig.ClientId);

        var response = await GetTokenFromAppleAsync(url, code, _appleAuthOptions.ExtensionConfig.ClientId, secret);
        Logger.LogInformation("Get token from apple: {Response}", response);

        if (response.Contains("error"))
        {
            throw new UserFriendlyException(response);
        }

        return JObject.Parse(response)["id_token"]?.ToString();
    }

    private async Task<string> GetTokenFromAppleAsync(string url, string code, string clientId, string clientSecret)
    {
        var client = _httpClientFactory.CreateClient();
        var parameters = new Dictionary<string, string>
        {
            { "client_id", clientId },
            { "client_secret", clientSecret },
            { "grant_type", "authorization_code" },
            { "code", code }
        };

        var content = new FormUrlEncodedContent(parameters);
        var response = await client.PostAsync(url, content);
        return await response.Content.ReadAsStringAsync();
    }

    private string GetSecret(string privateKey, string keyId, string teamId, string clientId)
    {
        var key = new ECDsaSecurityKey(ECDsa.Create());
        key.ECDsa.ImportPkcs8PrivateKey(Convert.FromBase64String(privateKey), out _);
        key.KeyId = keyId;

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = teamId,
            Audience = "https://appleid.apple.com",
            Subject = new ClaimsIdentity(new[] { new Claim("sub", clientId) }),
            NotBefore = DateTime.UtcNow,
            Expires = DateTime.UtcNow.AddMinutes(180),
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.EcdsaSha256)
        };

        var token = _jwtSecurityTokenHandler.CreateJwtSecurityToken(descriptor);
        var clientSecret = _jwtSecurityTokenHandler.WriteToken(token);

        return clientSecret;
    }
}