using System;
using System.Threading.Tasks;
using CAServer.Options;
using CAServer.Search;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;

namespace CAServer.Common;

public interface IBusinessAlertProvider
{
    public Task<bool> LoginRegisterFailureAlert(string sessionId, string dappName, string content);
}

public class BusinessAlertProvider : IBusinessAlertProvider, ISingletonDependency
{
    private readonly IDistributedCache<string> _distributedCache;
    private readonly IHttpClientService _httpClientService;
    private readonly ILogger<BusinessAlertProvider> _logger;
    private readonly BusinessAlertOptions _businessAlertOptions;


    public BusinessAlertProvider(
        IDistributedCache<string> distributedCache,
        IHttpClientService httpClientService,
        ILogger<BusinessAlertProvider> logger,
        IOptionsSnapshot<BusinessAlertOptions> businessAlertOptions)
    {
        _distributedCache = distributedCache;
        _httpClientService = httpClientService;
        _logger = logger;
        _businessAlertOptions = businessAlertOptions.Value;
    }

    private int loginRegisterTimeoutSeconds = 60;
    private int loginRegisterNumber = 10;

    public async Task<bool> LoginRegisterFailureAlert(string sessionId, string dappName, string content)
    {
        try
        {
            string key = $"{sessionId};{dappName ?? "default"}";
            string value = await _distributedCache.GetAsync(key) ?? "0";
            value = (int.Parse(value) + 1).ToString();
            await _distributedCache.SetAsync(key, value, new DistributedCacheEntryOptions { AbsoluteExpiration = DateTimeOffset.Now.AddSeconds(loginRegisterTimeoutSeconds) });

            int number = int.Parse(value);
            if (number < loginRegisterNumber)
            {
                return false;
            }

            await _distributedCache.RemoveAsync(key);
            await SendWebhook(sessionId, content, dappName);

            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "LoginRegisterFailureAlert error, sessionId = {0}, content = {1}", sessionId, content);
        }

        return false;
    }

    private long sendIntervalSeconds = 10;
    private long lastSendTime = DateTimeOffset.Now.ToUnixTimeSeconds();
    private string title = "Login or registration failed.";
    private async Task SendWebhook(string sessionId, string content, string dappName)
    {
        if (DateTimeOffset.Now.ToUnixTimeSeconds() - lastSendTime < sendIntervalSeconds)
        {
            return;
        }

        var msg = new
        {
            msg_type = "text",
            content = new
                { text = $" title : {title}\n sessionId : {sessionId} \n dappName : {dappName} \n content : {content}" }
        };
        await _httpClientService.PostAsync<object>(_businessAlertOptions.Webhook, msg);
        lastSendTime = DateTimeOffset.Now.ToUnixTimeSeconds();
    }
}