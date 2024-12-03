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
    public Task<bool> LoginRegisterFailureAlert(string indexName, GetListInput input);
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
    private string title = "Login or registration failed.";

    public async Task<bool> LoginRegisterFailureAlert(string indexName, GetListInput input)
    {
        try
        {
            string key = $"{indexName};{input.Filter};{input.DappName ?? "default"}";
            string value = await _distributedCache.GetOrAddAsync(
                key,
                async () => "1",
                () => new DistributedCacheEntryOptions
                {
                    AbsoluteExpiration = DateTimeOffset.Now.AddSeconds(loginRegisterTimeoutSeconds)
                }
            );
            int number = int.Parse(value);
            if (number < loginRegisterNumber)
            {
                return false;
            }

            await _distributedCache.RemoveAsync(key);

            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "LoginRegisterFailureAlert error, indexName = {0}, input = {1}", indexName, JsonConvert.SerializeObject(input));
        }

        return false;
    }

    private long sendIntervalSeconds = 10;
    private long lastSendTime = DateTimeOffset.Now.ToUnixTimeSeconds();

    private async Task SendWebhook(string title, string indexName, string filter, string dappName)
    {
        if (DateTimeOffset.Now.ToUnixTimeSeconds() - lastSendTime < sendIntervalSeconds)
        {
            return;
        }

        var msg = new
        {
            msg_type = "text",
            content = new
                { text = $" title : {title}\n indexName : {indexName} \n filter : {filter}\n dapp : {dappName}" }
        };
        await _httpClientService.PostAsync<object>(_businessAlertOptions.Webhook, msg);
        lastSendTime = DateTimeOffset.Now.ToUnixTimeSeconds();
    }
}