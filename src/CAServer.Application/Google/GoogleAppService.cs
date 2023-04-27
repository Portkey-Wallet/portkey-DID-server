using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using CAServer.Options;
using CAServer.Verifier;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;

namespace CAServer.Google;

public class GoogleAppService : IGoogleAppService, ISingletonDependency
{
    private readonly IDistributedCache<SendVerifierCodeInterfaceRequestCountCacheItem> _distributedCache;
    private readonly SendVerifierCodeRequestLimitOptions _sendVerifierCodeRequestLimitOptions;
    private readonly ILogger<GoogleAppService> _logger;
    private readonly GoogleRecaptchaOptions _googleRecaptchaOption;
    private readonly IHttpClientFactory _httpClientFactory;

    public GoogleAppService(IDistributedCache<SendVerifierCodeInterfaceRequestCountCacheItem> distributedCache,
        IOptions<SendVerifierCodeRequestLimitOptions> sendVerifierCodeRequestLimitOptions,
        ILogger<GoogleAppService> logger, IOptions<GoogleRecaptchaOptions> googleRecaptchaOption,
        IHttpClientFactory httpClientFactory)
    {
        _distributedCache = distributedCache;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _googleRecaptchaOption = googleRecaptchaOption.Value;
        _sendVerifierCodeRequestLimitOptions = sendVerifierCodeRequestLimitOptions.Value;
    }

    private const string SendVerifierCodeInterfaceRequestCountCacheKey =
        "SendVerifierCodeInterfaceRequestCountCacheKey";

    public async Task<bool> IsGoogleRecaptchaOpen(string userIpAddress)
    {
        var cacheItem =
            await _distributedCache.GetAsync(SendVerifierCodeInterfaceRequestCountCacheKey + ":" + userIpAddress);
        return cacheItem != null && cacheItem.SendVerifierCodeInterfaceRequestCount >=
            _sendVerifierCodeRequestLimitOptions.Limit;
    }

    public async Task<bool> GoogleRecaptchaTokenSuccessAsync(string recaptchaToken)
    {
        if (string.IsNullOrWhiteSpace(recaptchaToken))
        {
            _logger.LogDebug("Google Recaptcha Token is Empty");
            return false;
        }

        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("secret", _googleRecaptchaOption.Secret),
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
}