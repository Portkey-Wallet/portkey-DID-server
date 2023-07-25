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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Volo.Abp;
using Volo.Abp.Auditing;

namespace CAServer.AppleMigrate;

[RemoteService(false)]
[DisableAuditing]
public class AppleMigrateProvider : CAServerAppService, IAppleMigrateProvider
{
    private readonly JwtSecurityTokenHandler _jwtSecurityTokenHandler;
    private readonly AppleAuthOptions _oldAppleAuthOptions;
    private readonly IHttpClientFactory _httpClientFactory;
    private static string _oldAccessToken = string.Empty;
    private static string _accessToken = string.Empty;

    private static string _oldSecret = string.Empty;
    private static string _secret = string.Empty;

    public AppleMigrateProvider(JwtSecurityTokenHandler jwtSecurityTokenHandler,
        IOptions<AppleAuthOptions> appleAuthOptions,
        IHttpClientFactory httpClientFactory)
    {
        _jwtSecurityTokenHandler = jwtSecurityTokenHandler;
        _oldAppleAuthOptions = appleAuthOptions.Value;
        _httpClientFactory = httpClientFactory;
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
        if (_oldSecret.IsNullOrWhiteSpace() || _oldAccessToken.IsNullOrWhiteSpace()) await SetConfig();

        return new GetSubDto();
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
        if (_secret.IsNullOrWhiteSpace() || _accessToken.IsNullOrWhiteSpace()) await SetConfig();
        // get transfer_sub from userId.
        transferSub = "000995.r61f00f5d5b4a461ba35d2b19ce2e8b8a";
        var url = "https://appleid.apple.com/auth/usermigrationinfo";

        var dic = new Dictionary<string, string>
        {
            { "transfer_sub", transferSub },
            { "client_id", _oldAppleAuthOptions.ExtensionConfig.ClientId },
            { "client_secret", _secret }
        };

        var dto = await PostFormAsync<GetNewUserIdDto>(url, dic, _accessToken);
        return dto;
    }

    private async Task SetConfig()
    {
        _oldSecret = GetSecret(_oldAppleAuthOptions.ExtensionConfig.PrivateKey,
            _oldAppleAuthOptions.ExtensionConfig.KeyId,
            _oldAppleAuthOptions.ExtensionConfig.TeamId, _oldAppleAuthOptions.ExtensionConfig.ClientId);

        _secret = GetSecret(_oldAppleAuthOptions.ExtensionConfig.PrivateKey, _oldAppleAuthOptions.ExtensionConfig.KeyId,
            _oldAppleAuthOptions.ExtensionConfig.TeamId, _oldAppleAuthOptions.ExtensionConfig.ClientId);

        _oldAccessToken = await GetAccessToken(_oldAppleAuthOptions.ExtensionConfig.ClientId, _oldSecret);
        _accessToken = await GetAccessToken(_oldAppleAuthOptions.ExtensionConfig.ClientId, _oldSecret);
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

    public string GetSecret()
    {
        var secret = GetSecret(_oldAppleAuthOptions.ExtensionConfig.PrivateKey,
            _oldAppleAuthOptions.ExtensionConfig.KeyId,
            _oldAppleAuthOptions.ExtensionConfig.TeamId, _oldAppleAuthOptions.ExtensionConfig.ClientId);

        return secret;
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