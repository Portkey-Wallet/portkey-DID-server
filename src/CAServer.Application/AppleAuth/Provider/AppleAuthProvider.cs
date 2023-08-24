using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading.Tasks;
using CAServer.Common;
using CAServer.Commons;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace CAServer.AppleAuth.Provider;

public interface IAppleAuthProvider
{
    string GetSecret();
}

public class AppleAuthProvider : IAppleAuthProvider, ISingletonDependency
{
    private readonly JwtSecurityTokenHandler _jwtSecurityTokenHandler;
    private readonly AppleAuthOptions _appleAuthOptions;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AppleAuthProvider> _logger;

    public AppleAuthProvider(JwtSecurityTokenHandler jwtSecurityTokenHandler,
        IOptionsSnapshot<AppleAuthOptions> appleAuthOptions,
        IHttpClientFactory httpClientFactory,
        ILogger<AppleAuthProvider> logger)
    {
        _jwtSecurityTokenHandler = jwtSecurityTokenHandler;
        _appleAuthOptions = appleAuthOptions.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    private async Task<bool> RevokeAsync(string token)
    {
        var client = _httpClientFactory.CreateClient();
        var parameters = new Dictionary<string, string>
        {
            { "client_id", _appleAuthOptions.ExtensionConfig.ClientId },
            { "client_secret", GetSecret() },
            { "token", token },
            { "token_type_hint", "access_token" }
        };

        var content = new FormUrlEncodedContent(parameters);
        var response = await client.PostAsync(CommonConstant.AppleRevokeUrl, content);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("request not success");
            return false;
        }

        return true;
    }

    public string GetSecret()
    {
        return GetSecret(_appleAuthOptions.ExtensionConfig.PrivateKey, _appleAuthOptions.ExtensionConfig.KeyId,
            _appleAuthOptions.ExtensionConfig.TeamId, _appleAuthOptions.ExtensionConfig.ClientId);
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