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
using CAServer.Grains;
using CAServer.Grains.Grain.UserExtraInfo;
using CAServer.Verifier.Etos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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

    public AppleAuthAppService(IHttpClientFactory httpClientFactory,
        IOptions<AppleAuthOptions> appleAuthVerifyOption,
        IDistributedEventBus distributedEventBus,
        IClusterClient clusterClient,
        IHttpClientService httpClientService,
        JwtSecurityTokenHandler jwtSecurityTokenHandler,
        IHttpContextAccessor httpContextAccessor,
        IAppleUserProvider appleUserProvider)
    {
        _httpClientFactory = httpClientFactory;
        _appleAuthOptions = appleAuthVerifyOption.Value;
        _clusterClient = clusterClient;
        _distributedEventBus = distributedEventBus;
        _httpClientService = httpClientService;
        _jwtSecurityTokenHandler = jwtSecurityTokenHandler;
        _httpContextAccessor = httpContextAccessor;
        _appleUserProvider = appleUserProvider;
    }

    public async Task ReceiveAsync(AppleAuthDto appleAuthDto)
    {
        Logger.LogInformation($"Apple token:  {JsonConvert.SerializeObject(appleAuthDto)}");

        var identityToken = appleAuthDto.Id_token;
        if (string.IsNullOrEmpty(appleAuthDto.Id_token))
        {
            identityToken = await GetTokenAsync(appleAuthDto.Code);
        }

        var securityToken = await ValidateTokenAsync(identityToken);
        var jwtPayload = ((JwtSecurityToken)securityToken).Payload;

        if (string.IsNullOrWhiteSpace(appleAuthDto.User))
        {
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

    public IActionResult ReceiveTestAsync(AppleAuthDto appleAuthDto)
    {
        if (appleAuthDto == null)
        {
            Logger.LogError($"ReceiveTest: appleAuthDto is null.");
            throw new UserFriendlyException("appleAuthDto is null");
        }
        
        if (string.IsNullOrEmpty(appleAuthDto.State))
        {
            Logger.LogError($"ReceiveTest: not from apple.{_httpContextAccessor.HttpContext.Request.Path}");
            throw new UserFriendlyException("not from apple");
        }
        
        Logger.LogInformation($"ReceiveTest:  {JsonConvert.SerializeObject(appleAuthDto)}");
        return new RedirectResult($"{_appleAuthOptions.RedirectUrl}?id_token={appleAuthDto.Id_token}");
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
            ObjectMapper.Map<UserExtraInfoGrainDto, UserExtraInfoEto>(grainDto));
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
            Logger.LogError(e, e.Message);
            throw new UserFriendlyException("The token has expired.");
        }
        catch (SecurityTokenException e)
        {
            Logger.LogError(e, e.Message);
            throw new UserFriendlyException("Valid token fail.");
        }
        catch (ArgumentException e)
        {
            Logger.LogError(e, e.Message);
            throw new UserFriendlyException("Invalid token.");
        }
        catch (Exception e)
        {
            Logger.LogError(e, e.Message);
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
        var secret = GetSecret(_appleAuthOptions.ExtensionConfig.PrivateKey, _appleAuthOptions.ExtensionConfig.KeyId,
            _appleAuthOptions.ExtensionConfig.TeamId, _appleAuthOptions.ExtensionConfig.ClientId);

        var response = await GetTokenFromAppleAsync(url, code, _appleAuthOptions.ExtensionConfig.ClientId, secret);
        Logger.LogInformation("Get token from apple: {response}", response);

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