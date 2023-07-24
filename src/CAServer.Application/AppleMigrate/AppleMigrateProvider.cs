using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
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
    private readonly AppleAuthOptions _appleAuthOptions;
    private readonly IHttpClientFactory _httpClientFactory;

    public AppleMigrateProvider(JwtSecurityTokenHandler jwtSecurityTokenHandler, IOptions<AppleAuthOptions> appleAuthOptions,
        IHttpClientFactory httpClientFactory)
    {
        _jwtSecurityTokenHandler = jwtSecurityTokenHandler;
        _appleAuthOptions = appleAuthOptions.Value;
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
        var clientSecret = GetSecret();
        var accessToken = await GetAccessToken();
        return new GetSubDto();
    }

    /// <summary>
    /// this step is use new team config.
    /// step in get transfer_sub:
    ///     1. get client secret through new config.
    ///     2. get access token from apple.
    ///     3. get new userId from apple use transfer_sub as param.
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    public Task<GetNewUserIdDto> GetNewUserIdAsync(string userId)
    {
        throw new System.NotImplementedException();
    }

    public async Task<string> GetAccessToken()
    {
        var url = "https://appleid.apple.com/auth/token";
        var clientSecret = GetSecret();

        var dic = new Dictionary<string, string>
        {
            { "grant_type", "client_credentials" },
            { "scope", "user.migration" },
            { "client_id", _appleAuthOptions.ExtensionConfig.ClientId },
            { "client_secret", clientSecret }
        };

        var dto = await PostAsync<AccessDto>(url, dic);
        return dto.AccessToken;
    }

    private async Task<T> PostAsync<T>(string url, Dictionary<string, string> paramDic, string accessToken = "")
        where T : class
    {
        var requestContent = new StringContent(
            JsonConvert.SerializeObject(paramDic, Formatting.None),
            Encoding.UTF8,
            MediaTypeNames.Application.Json);

        var client = _httpClientFactory.CreateClient();

        if (string.IsNullOrWhiteSpace(accessToken))
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
        var secret = GetSecret(_appleAuthOptions.ExtensionConfig.PrivateKey, _appleAuthOptions.ExtensionConfig.KeyId,
            _appleAuthOptions.ExtensionConfig.TeamId, _appleAuthOptions.ExtensionConfig.ClientId);

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