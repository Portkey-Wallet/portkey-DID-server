using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using CAServer.Cache;
using CAServer.Google.Dtos;
using CAServer.Options;
using CAServer.Signature.Options;
using CAServer.Signature.Provider;
using CAServer.Verifier;
using CAServer.Verifier.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Volo.Abp.DependencyInjection;

namespace CAServer.Google;

public class GoogleAppService : IGoogleAppService, ISingletonDependency
{
    private readonly ICacheProvider _cacheProvider;
    private readonly SendVerifierCodeRequestLimitOptions _sendVerifierCodeRequestLimitOptions;
    private readonly ILogger<GoogleAppService> _logger;
    private readonly GoogleRecaptchaOptions _googleRecaptchaOption;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly JwtSecurityTokenHandler _jwtSecurityTokenHandler;
    private readonly FireBaseAppCheckOptions _fireBaseAppCheckOptions;
    private readonly ISecretProvider _secretProvider;
    private readonly IOptionsMonitor<SignatureServerOptions> _signatureOptions;

    public GoogleAppService(
        IOptionsSnapshot<SendVerifierCodeRequestLimitOptions> sendVerifierCodeRequestLimitOptions,
        ILogger<GoogleAppService> logger, IOptions<GoogleRecaptchaOptions> googleRecaptchaOption,
        IHttpClientFactory httpClientFactory, ICacheProvider cacheProvider,
        JwtSecurityTokenHandler jwtSecurityTokenHandler, IOptionsSnapshot<FireBaseAppCheckOptions> fireBaseAppCheckOptions, ISecretProvider secretProvider, IOptionsMonitor<SignatureServerOptions> signatureOptions)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _cacheProvider = cacheProvider;
        _jwtSecurityTokenHandler = jwtSecurityTokenHandler;
        _secretProvider = secretProvider;
        _signatureOptions = signatureOptions;
        _fireBaseAppCheckOptions = fireBaseAppCheckOptions.Value;
        _googleRecaptchaOption = googleRecaptchaOption.Value;
        _sendVerifierCodeRequestLimitOptions = sendVerifierCodeRequestLimitOptions.Value;
    }

    private const string SendVerifierCodeInterfaceRequestCountCacheKey =
        "SendVerifierCodeInterfaceRequestCountCacheKey";

    public async Task<bool> IsGoogleRecaptchaOpenAsync(string userIpAddress, OperationType type)
    {
        var cacheCount =
            await _cacheProvider.Get(SendVerifierCodeInterfaceRequestCountCacheKey + ":" + userIpAddress);
        if (cacheCount.IsNullOrEmpty)
        {
            cacheCount = 0;
        }

        _logger.LogDebug("cacheItem is {item}, limit is {limit}", JsonConvert.SerializeObject(cacheCount),
            _sendVerifierCodeRequestLimitOptions.Limit);
        if (!int.TryParse(cacheCount, out var count))
        {
            return false;
        }

        return type switch
        {
            OperationType.CreateCAHolder => true,
            _ => count >= _sendVerifierCodeRequestLimitOptions.Limit
        };
    }

    public async Task<bool> IsGoogleRecaptchaTokenValidAsync(string recaptchaToken, PlatformType platformType)
    {
        var key = _signatureOptions.CurrentValue.KeyIds.GoogleRecaptcha + platformType;
        var secret = await _secretProvider.GetSecretWithCacheAsync(key);
        if (string.IsNullOrWhiteSpace(recaptchaToken))
        {
            _logger.LogDebug("Google Recaptcha Token is Empty");
            return false;
        }

        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("secret", secret),
            new KeyValuePair<string, string>("response", recaptchaToken)
        });
        var client = _httpClientFactory.CreateClient();
        var response = await client.PostAsync(_googleRecaptchaOption.VerifyUrl, content);
        _logger.LogDebug("response is {response}", JsonConvert.SerializeObject(response));
        if (!response.IsSuccessStatusCode)
        {
            return false;
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        _logger.LogDebug(" VerifyGoogleRecaptchaToken responseContent is {responseContent}", responseContent);
        return responseContent.Contains("\"success\": true");
    }

    public async Task<ValidateTokenResponse> ValidateTokenAsync(string rcToken, string acToken,
        PlatformType platformType)
    {
        if (string.IsNullOrEmpty(rcToken) && string.IsNullOrWhiteSpace(acToken))
        {
            return new ValidateTokenResponse();
        }

        if (!string.IsNullOrWhiteSpace(rcToken))
        {
            try
            {
                var isValidSuccess = await IsGoogleRecaptchaTokenValidAsync(rcToken, platformType);
                if (isValidSuccess)
                {
                    return new ValidateTokenResponse
                    {
                        RcValidResult = true,
                    };
                }
            }
            catch (Exception e)
            {
                _logger.LogError("Google Recaptcha Token Valid Error {error}", e.Message);
                return new ValidateTokenResponse();
            }
        }

        if (string.IsNullOrWhiteSpace(acToken))
        {
            return new ValidateTokenResponse();
        }

        try
        {
            await VerifyAppCheckTokenAsync(acToken);
            return new ValidateTokenResponse
            {
                AcValidResult = true
            };
            //var firebase = jwtPayload.FirstOrDefault(t => t.Key == "firebase");
        }
        catch (Exception e)
        {
            _logger.LogError("Google App Check Token Valid Error {error}", e.Message);
            return new ValidateTokenResponse();
        }
    }

    public async Task<SecurityToken> VerifyAppCheckTokenAsync(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return new JwtSecurityToken();
        }
        
        var jwtToken = _jwtSecurityTokenHandler.ReadJwtToken(token);
        var kid = jwtToken.Header.Kid;
        var client = new HttpClient();
        var jwksResponse = await client.GetStringAsync(_fireBaseAppCheckOptions.RequestUrl);
        var response = JsonConvert.DeserializeObject<Response>(jwksResponse);
        var jwk = new JsonWebKey(
            JsonConvert.SerializeObject(response.Keys.FirstOrDefault(t => t.Kid == kid)));
        var validateParameter = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _fireBaseAppCheckOptions.ValidIssuer,
            ValidateAudience = true,
            ValidAudiences = _fireBaseAppCheckOptions.ValidAudiences,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = jwk
        };
        _jwtSecurityTokenHandler.ValidateToken(token, validateParameter,
            out var validatedToken);
        return validatedToken;
    }
}

public class Response
{
    public List<GoogleKeys> Keys { get; set; }
}

public class GoogleKeys
{
    public string Kty { get; set; }
    public string Kid { get; set; }
    public string Use { get; set; }
    public string Alg { get; set; }
    public string N { get; set; }
    public string E { get; set; }
    
}