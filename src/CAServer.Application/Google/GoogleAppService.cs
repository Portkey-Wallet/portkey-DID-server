using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CAServer.Cache;
using CAServer.Google.Utils;
using CAServer.Options;
using CAServer.Verifier;
using FirebaseAdmin.Auth;
using Google.Apis.Auth;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace CAServer.Google;

public class GoogleAppService : IGoogleAppService, ISingletonDependency
{
    private readonly ICacheProvider _cacheProvider;
    private readonly SendVerifierCodeRequestLimitOptions _sendVerifierCodeRequestLimitOptions;
    private readonly ILogger<GoogleAppService> _logger;
    private readonly GoogleRecaptchaOptions _googleRecaptchaOption;
    private readonly IHttpClientFactory _httpClientFactory;

    public GoogleAppService(
        IOptionsSnapshot<SendVerifierCodeRequestLimitOptions> sendVerifierCodeRequestLimitOptions,
        ILogger<GoogleAppService> logger, IOptions<GoogleRecaptchaOptions> googleRecaptchaOption,
        IHttpClientFactory httpClientFactory, ICacheProvider cacheProvider)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _cacheProvider = cacheProvider;
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
        var platformTypeName = platformType.ToString();
        var getSuccess = _googleRecaptchaOption.SecretMap.TryGetValue(platformTypeName, out var secret);
        if (!getSuccess)
        {
            throw new UserFriendlyException("Google Recaptcha Secret Not Found");
        }

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
        _logger.LogDebug("VerifyGoogleRecaptchaToken content is {content}", content.ToString());
        var client = _httpClientFactory.CreateClient();
        var response = await client.PostAsync(_googleRecaptchaOption.VerifyUrl, content);
        _logger.LogDebug("response is {response}", response.ToString());
        if (!response.IsSuccessStatusCode)
        {
            return false;
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        _logger.LogDebug(" VerifyGoogleRecaptchaToken responseContent is {responseContent}", responseContent);
        return responseContent.Contains("\"success\": true");
    }

    public Task<bool> VerifyFireBaseTokenAsync(string token)
    {
        var segments = !string.IsNullOrEmpty(token) ? token.Split(new char[1]
        {
            '.'
        }) : throw new ArgumentException( "token  must not be null or empty.");
        var header = segments.Length == 3 ? JwtUtils.Decode<JsonWebSignature.Header>(segments[0]) : throw new Exception("Incorrect number of segments in token.");
        
        var payload = JwtUtils.Decode<PayLoad>(segments[1]);
        
        
        
        
    }
    
        
    private async Task VerifySignatureAsync(
        string[] segments,
        string keyId,
        CancellationToken cancellationToken)
    {
       
        {
            byte[] hash;
            using (SHA256 shA256 = SHA256.Create())
                hash = shA256.ComputeHash(Encoding.ASCII.GetBytes(segments[0] + "." + segments[1]));
            byte[] signature = JwtUtils.Base64DecodeToBytes(segments[2]);
            if (!(await this.keySource.GetPublicKeysAsync(cancellationToken).ConfigureAwait(false)).Any<PublicKey>((Func<PublicKey, bool>) (key => key.Id == keyId && key.RSA.VerifyHash(hash, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1))))
                throw new Exception("Failed to verify token signature.");
        }
    }
    
}


public class PayLoad
{
    [JsonProperty("iss")]
    public string Issuer { get; set; }

    [JsonProperty("sub")]
    public string Subject { get; set; }

    [JsonProperty("aud")]
    public List<string> Audiences { get; set; }

    [JsonProperty("exp")]
    public long ExpirationTimeSeconds { get; set; }

    [JsonProperty("iat")]
    public long IssuedAtTimeSeconds { get; set; }

    [JsonProperty("firebase")]
    public FirebaseInfo Firebase { get; set; }

    [JsonIgnore]
    public IReadOnlyDictionary<string, object> Claims { get; set; }
}

public class FirebaseInfo
{
    [JsonProperty("tenant")]
    public string Tenant { get; set; }
}