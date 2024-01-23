using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading.Tasks;
using CAServer.AppleVerify;
using CAServer.Common;
using CAServer.Commons;
using CAServer.SecurityServer;
using CAServer.Signature.Provider;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace CAServer.AppleAuth.Provider;

public interface IAppleAuthProvider
{
    Task<bool> RevokeAsync(string identityToken);
    Task<SecurityToken> ValidateTokenAsync(string identityToken);
    Task<bool> VerifyAppleId(string identityToken, string appleId);
}

public class AppleAuthProvider : IAppleAuthProvider, ISingletonDependency
{
    private readonly JwtSecurityTokenHandler _jwtSecurityTokenHandler;
    private readonly AppleAuthOptions _appleAuthOptions;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AppleAuthProvider> _logger;
    private readonly ISecretProvider _secretProvider;

    public AppleAuthProvider(JwtSecurityTokenHandler jwtSecurityTokenHandler,
        IOptionsSnapshot<AppleAuthOptions> appleAuthOptions,
        IHttpClientFactory httpClientFactory,
        ILogger<AppleAuthProvider> logger, ISecretProvider secretProvider)
    {
        _jwtSecurityTokenHandler = jwtSecurityTokenHandler;
        _appleAuthOptions = appleAuthOptions.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _secretProvider = secretProvider;
    }

    public async Task<bool> VerifyAppleId(string identityToken, string appleId)
    {
        try
        {
            var securityToken = await ValidateTokenAsync(identityToken);
            var jwtPayload = ((JwtSecurityToken)securityToken).Payload;
            return jwtPayload.Sub == appleId;
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);
            return false;
        }
    }

    public async Task<SecurityToken> ValidateTokenAsync(string identityToken)
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
            _logger.LogError(e, e.Message);
            throw new UserFriendlyException("The token has expired.");
        }
        catch (SecurityTokenException e)
        {
            _logger.LogError(e, e.Message);
            throw new UserFriendlyException("Valid token fail.");
        }
        catch (ArgumentException e)
        {
            _logger.LogError(e, e.Message);
            throw new UserFriendlyException("Invalid token.");
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);
            throw new UserFriendlyException(e.Message);
        }
    }

    private async Task<AppleKeys> GetAppleKeyFormAppleAsync()
    {
        var appleKeyUrl = "https://appleid.apple.com/auth/keys";
        var response = await _httpClientFactory.CreateClient().GetStringAsync(appleKeyUrl);
        return JsonConvert.DeserializeObject<AppleKeys>(response);
    }

    public async Task<bool> RevokeAsync(string token)
    {
        var client = _httpClientFactory.CreateClient();
        var secret = await _secretProvider.GetAppleAuthSignatureAsync(_appleAuthOptions.ExtensionConfig.KeyId,
            _appleAuthOptions.ExtensionConfig.TeamId, _appleAuthOptions.ExtensionConfig.ClientId);
        var parameters = new Dictionary<string, string>
        {
            { "client_id", _appleAuthOptions.ExtensionConfig.ClientId },
            { "client_secret", secret },
            { "token", token },
            { "token_type_hint", "access_token" }
        };

        var content = new FormUrlEncodedContent(parameters);
        var response = await client.PostAsync(CommonConstant.AppleRevokeUrl, content);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("revoke not success, token: {token}, content:{content}", token,
                response.Content.ReadAsStringAsync());
            return false;
        }

        return true;
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