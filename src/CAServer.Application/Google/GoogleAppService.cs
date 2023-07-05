using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using CAServer.Cache;
using CAServer.Options;
using CAServer.Verifier;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
    private const string CurrentVersion = "v1.3.0";

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
            OperationType.SocialRecovery => count >= _sendVerifierCodeRequestLimitOptions.Limit,
            OperationType.AddGuardian => count >= _sendVerifierCodeRequestLimitOptions.Limit,
            OperationType.RemoveGuardian=> count >= _sendVerifierCodeRequestLimitOptions.Limit,
            OperationType.RemoveOtherManagerInfo => count >= _sendVerifierCodeRequestLimitOptions.Limit,
            OperationType.UpdateGuardian => count >= _sendVerifierCodeRequestLimitOptions.Limit,
            OperationType.SetLoginGuardian => count >= _sendVerifierCodeRequestLimitOptions.Limit,
            _ => false
        };
    }

    public async Task<bool> IsGoogleRecaptchaTokenValidAsync(string recaptchaToken)
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