using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using CAServer.Cache;
using CAServer.Options;
using CAServer.Verifier;
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
        ILogger<GoogleAppService> logger, IOptionsSnapshot<GoogleRecaptchaOptions> googleRecaptchaOption,
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
            _logger.LogError("Google Recaptcha Secret Not Found");
            return false;
        }

        var responseContent = await GoogleRecaptchaAsync(secret, recaptchaToken);
        _logger.LogDebug(" VerifyGoogleRecaptchaToken responseContent is {responseContent}", responseContent);
        return !string.IsNullOrEmpty(responseContent) && responseContent.Contains("\"success\": true");
    }

    public async Task<bool> GoogleRecaptchaV3Async(string inputRecaptchaToken,
        PlatformType platformType = PlatformType.WEB)
    {
        var platformTypeName = platformType.ToString();
        var getSuccess = _googleRecaptchaOption.V3SecretMap.TryGetValue(platformTypeName, out var secret);
        if (!getSuccess)
        {
            _logger.LogError("Google Recaptcha Secret Not Found");
            return false;
        }

        var responseContent = await GoogleRecaptchaAsync(secret, inputRecaptchaToken);
        _logger.LogDebug(" VerifyGoogleRecaptchaToken responseContent is {responseContent}", responseContent);
        if (string.IsNullOrEmpty(responseContent))
        {
            return false;
        }

        var googleReCaptchaResponse = JsonConvert.DeserializeObject<RecaptchaResponse>(responseContent);
        return googleReCaptchaResponse.Score > _googleRecaptchaOption.Score;
    }

    private async Task<string> GoogleRecaptchaAsync(string secret, string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogDebug("Google Recaptcha Token is Empty");
            return string.Empty;
        }

        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("secret", secret),
            new KeyValuePair<string, string>("response", token)
        });
        var client = _httpClientFactory.CreateClient();
        var response = await client.PostAsync(_googleRecaptchaOption.VerifyUrl, content);
        if (!response.IsSuccessStatusCode)
        {
            return string.Empty;
        }

        var responseContent = await response.Content.ReadAsStringAsync();

        return responseContent;
    }
}