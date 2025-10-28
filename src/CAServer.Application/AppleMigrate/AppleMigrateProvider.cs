using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using CAServer.AppleAuth;
using CAServer.AppleMigrate.Dtos;
using CAServer.AppleMigrate.Dtos.AppleDtos;
using CAServer.Commons;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.Caching;

namespace CAServer.AppleMigrate;

[RemoteService(false)]
[DisableAuditing]
public class AppleMigrateProvider : CAServerAppService, IAppleMigrateProvider
{
    private readonly JwtSecurityTokenHandler _jwtSecurityTokenHandler;
    private readonly AppleAuthOptions _oldAppleAuthOptions;
    private readonly AppleAuthTransferredOptions _transferredAppleAuthOptions;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IDistributedCache<AppleUserTransfer> _distributedCache;
    
    private static string _oldAccessToken = string.Empty;
    private static string _accessToken = string.Empty;

    private static string _oldSecret = string.Empty;
    private static string _secret = string.Empty;

    public AppleMigrateProvider(JwtSecurityTokenHandler jwtSecurityTokenHandler,
        IOptions<AppleAuthOptions> appleAuthOptions,
        IHttpClientFactory httpClientFactory,
        IDistributedCache<AppleUserTransfer> distributedCache,
        IOptions<AppleAuthTransferredOptions> transferredAppleAuthOptions)
    {
        _jwtSecurityTokenHandler = jwtSecurityTokenHandler;
        _oldAppleAuthOptions = appleAuthOptions.Value;
        _transferredAppleAuthOptions = transferredAppleAuthOptions.Value;
        _httpClientFactory = httpClientFactory;
        _distributedCache = distributedCache;
    }

    /// <summary>
    /// get transfer_sub is use old team config.
    /// step in get transfer_sub:
    ///     1. get client secret through old config.
    ///     2. get access token from apple.
    ///     3. get transfer_sub from apple use userId as param.
    /// </summary>
    /// <param name="userId">userId in old team</param>
    /// <returns></returns>
    public async Task<GetSubDto> GetSubAsync(string userId)
    {
        try
        {
            if (_oldSecret.IsNullOrWhiteSpace() || _oldAccessToken.IsNullOrWhiteSpace() ||
                _secret.IsNullOrWhiteSpace() || _accessToken.IsNullOrWhiteSpace()) await SetConfig();

            var url = "https://appleid.apple.com/auth/usermigrationinfo";

            var dic = new Dictionary<string, string>
            {
                { "sub", userId },
                { "target", _transferredAppleAuthOptions.ExtensionConfig.TeamId },
                { "client_id", _oldAppleAuthOptions.ExtensionConfig.ClientId },
                { "client_secret", _oldSecret }
            };

            var dto = await PostFormAsync<GetSubDto>(url, dic, _oldAccessToken);

            return new GetSubDto
            {
                Sub = dto.Sub,
                UserId = userId
            };
        }
        catch (Exception e)
        {
            Logger.LogError("get sub from apple error. userId:{userId}, message:{message}", userId, e.Message);
        }

        return new GetSubDto
        {
            Sub = "",
            UserId = userId
        };
    }

    /// <summary>
    /// this step is use new team config.
    /// step in get transfer_sub:
    ///     1. get client secret through new config.
    ///     2. get access token from apple.
    ///     3. get new userId from apple use transfer_sub as param.
    /// </summary>
    /// <param name="transferSub"></param>
    /// <returns></returns>
    public async Task<GetNewUserIdDto> GetNewUserIdAsync(string transferSub)
    {
        if (_oldSecret.IsNullOrWhiteSpace() || _oldAccessToken.IsNullOrWhiteSpace() ||
            _secret.IsNullOrWhiteSpace() || _accessToken.IsNullOrWhiteSpace()) await SetConfig();
        
        var url = "https://appleid.apple.com/auth/usermigrationinfo";

        var dic = new Dictionary<string, string>
        {
            { "transfer_sub", transferSub },
            { "client_id", _transferredAppleAuthOptions.ExtensionConfig.ClientId },
            { "client_secret", _secret }
        };

        var dto = await PostFormAsync<GetNewUserIdDto>(url, dic, _accessToken);
        return dto;
    }

    public async Task<int> SetTransferSubAsync()
    {
        var count = 0;
        var userTransfer = await _distributedCache.GetAsync(CommonConstant.AppleUserTransferKey);
        if (userTransfer?.AppleUserTransferInfos == null || userTransfer?.AppleUserTransferInfos.Count <= 0)
        {
            throw new UserFriendlyException("in SetTransferSubAsync,  all user info not in cache.");
        }

        foreach (var userTransferInfo in userTransfer.AppleUserTransferInfos)
        {
            try
            {
                if (userTransferInfo == null || userTransferInfo.UserId.IsNullOrWhiteSpace())
                {
                    continue;
                }

                if (!userTransferInfo.TransferSub.IsNullOrWhiteSpace())
                {
                    continue;
                }

                var subDto = await GetSubAsync(userTransferInfo.UserId);
                if (subDto.Sub.IsNullOrWhiteSpace())
                {
                    Logger.LogWarning("get sub fail: {userId}", userTransferInfo.UserId);
                    continue;
                }

                userTransferInfo.TransferSub = subDto.Sub;
                count++;
            }
            catch (Exception e)
            {
                Logger.LogError(e.Message);
            }
        }

        await _distributedCache.SetAsync(CommonConstant.AppleUserTransferKey, new AppleUserTransfer()
        {
            AppleUserTransferInfos = userTransfer.AppleUserTransferInfos
        }, new DistributedCacheEntryOptions()
        {
            AbsoluteExpiration = DateTime.UtcNow.AddYears(10)
        });

        return count;
    }

    public async Task<int> SetNewUserInfoAsync()
    {
        var count = 0;
        var userTransfer = await _distributedCache.GetAsync(CommonConstant.AppleUserTransferKey);
        if (userTransfer?.AppleUserTransferInfos == null || userTransfer?.AppleUserTransferInfos.Count <= 0)
        {
            throw new UserFriendlyException("in SetTransferSubAsync,  all user info not in cache.");
        }

        foreach (var userTransferInfo in userTransfer.AppleUserTransferInfos)
        {
            try
            {
                if (userTransferInfo == null || userTransferInfo.UserId.IsNullOrWhiteSpace())
                {
                    continue;
                }

                if (userTransferInfo.TransferSub.IsNullOrWhiteSpace())
                {
                    continue;
                }

                if (!userTransferInfo.Sub.IsNullOrWhiteSpace())
                {
                    continue;
                }

                var newUserIdDto = await GetNewUserIdAsync(userTransferInfo.TransferSub);
                if (newUserIdDto.Sub.IsNullOrWhiteSpace())
                {
                    Logger.LogWarning("get sub fail: userId: {userId}, transferSub:{transferSub}",
                        userTransferInfo.UserId, userTransferInfo.TransferSub);
                    continue;
                }

                userTransferInfo.Sub = newUserIdDto.Sub;
                if (!newUserIdDto.Email.IsNullOrWhiteSpace())
                    userTransferInfo.Email = newUserIdDto.Email;

                if (newUserIdDto.Is_private_email)
                    userTransferInfo.IsPrivateEmail = newUserIdDto.Is_private_email;

                count++;
            }
            catch (Exception e)
            {
                Logger.LogError(
                    "SetNewUserInfoAsync error: userId:{userId}, transferSub:{transferSub}, message:{message}",
                    userTransferInfo?.UserId, userTransferInfo?.TransferSub, e.Message);
            }
        }

        await _distributedCache.SetAsync(CommonConstant.AppleUserTransferKey, new AppleUserTransfer()
        {
            AppleUserTransferInfos = userTransfer.AppleUserTransferInfos
        }, new DistributedCacheEntryOptions()
        {
            AbsoluteExpiration = DateTime.UtcNow.AddYears(100)
        });

        return count;
    }

    private async Task SetConfig()
    {
        _oldSecret = GetSecret(_oldAppleAuthOptions.ExtensionConfig.PrivateKey,
            _oldAppleAuthOptions.ExtensionConfig.KeyId,
            _oldAppleAuthOptions.ExtensionConfig.TeamId, _oldAppleAuthOptions.ExtensionConfig.ClientId);
        Logger.LogInformation("old secret. {secret}", _oldSecret);

        _secret = GetSecret(_transferredAppleAuthOptions.ExtensionConfig.PrivateKey,
            _transferredAppleAuthOptions.ExtensionConfig.KeyId,
            _transferredAppleAuthOptions.ExtensionConfig.TeamId, _transferredAppleAuthOptions.ExtensionConfig.ClientId);
        Logger.LogInformation("transferred secret. {secret}", _secret);

        _oldAccessToken = await GetAccessToken(_oldAppleAuthOptions.ExtensionConfig.ClientId, _oldSecret);
        Logger.LogInformation("get old access token success. {token}", _oldAccessToken);
        _accessToken = await GetAccessToken(_transferredAppleAuthOptions.ExtensionConfig.ClientId, _secret);
        Logger.LogInformation("get transferred access token success. {token}", _accessToken);
    }

    public async Task<string> GetAccessToken(string clientId, string clientSecret)
    {
        var url = "https://appleid.apple.com/auth/token";

        var dic = new Dictionary<string, string>
        {
            { "grant_type", "client_credentials" },
            { "scope", "user.migration" },
            { "client_id", clientId },
            { "client_secret", clientSecret }
        };

        var dto = await PostFormAsync<AccessDto>(url, dic);

        if (dto == null)
            throw new UserFriendlyException(
                $"get access token success, clientId: {clientId}, clientSecret:{clientSecret}");

        Logger.LogInformation(
            "get access token success, clientId: {clientId}, clientSecret:{clientSecret}, accessToken:{accessToken}",
            clientId, clientSecret, dto.AccessToken);
        return dto.AccessToken;
    }

    private async Task<T> PostFormAsync<T>(string url, Dictionary<string, string> paramDic, string accessToken = "")
        where T : class
    {
        var client = _httpClientFactory.CreateClient();

        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            client.DefaultRequestHeaders.Add(HeaderNames.Authorization, $"Bearer {accessToken}");
        }

        var param = new List<KeyValuePair<string, string>>();
        if (paramDic is { Count: > 0 })
        {
            param.AddRange(paramDic.ToList());
        }

        var response = await client.PostAsync(url, new FormUrlEncodedContent(param));
        var content = await response.Content.ReadAsStringAsync();

        if (response.StatusCode != HttpStatusCode.OK)
        {
            Logger.LogError("Response status code not good, code:{code}, message: {message}, params:{param}",
                response.StatusCode, content, JsonConvert.SerializeObject(paramDic));
            return null;
        }

        return JsonConvert.DeserializeObject<T>(content);
    }

    private async Task<T> PostJsonAsync<T>(string url, Dictionary<string, string> paramDic, string accessToken = "")
        where T : class
    {
        var requestContent = new StringContent(
            JsonConvert.SerializeObject(paramDic, Formatting.None),
            Encoding.UTF8,
            MediaTypeNames.Application.Json);

        var client = _httpClientFactory.CreateClient();

        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            client.DefaultRequestHeaders.Add(HeaderNames.Authorization, accessToken);
        }

        var response = await client.PostAsync(url, requestContent);
        var content = await response.Content.ReadAsStringAsync();

        if (response.StatusCode != HttpStatusCode.OK)
        {
            Logger.LogError("Response status code not good, code:{code}, message: {message}, params:{param}",
                response.StatusCode, content, JsonConvert.SerializeObject(paramDic));
            return null;
        }

        return JsonConvert.DeserializeObject<T>(content);
    }

    public async Task<Dictionary<string, string>> GetSecretAndAccessToken()
    {
        var result = new Dictionary<string, string>();
        if (_oldSecret.IsNullOrWhiteSpace() || _oldAccessToken.IsNullOrWhiteSpace() ||
            _secret.IsNullOrWhiteSpace() || _accessToken.IsNullOrWhiteSpace())
        {
            await SetConfig();
        }

        result.Add("transferringSecret", _oldSecret);
        result.Add("transferringAccessToken", _oldAccessToken);
        result.Add("transferredSecret", _secret);
        result.Add("transferredAccessToken", _accessToken);

        return result;
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

    public async Task<AppleUserTransferInfo> GetTransferInfoFromCache(string userId)
    {
        if (userId.IsNullOrWhiteSpace())
        {
            throw new UserFriendlyException("userId is must");
        }

        var userTransfer = await _distributedCache.GetAsync(CommonConstant.AppleUserTransferKey);
        if (userTransfer?.AppleUserTransferInfos == null || userTransfer?.AppleUserTransferInfos.Count <= 0)
        {
            throw new UserFriendlyException("in SetTransferSubAsync,  all user info not in cache.");
        }

        return userTransfer.AppleUserTransferInfos.FirstOrDefault(t => t.UserId == userId);
    }
}