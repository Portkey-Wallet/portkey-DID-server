using System;
using System.Threading.Tasks;
using CAServer.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace CAServer.Common;

public interface IBusinessAlertProvider
{
    public Task SendWebhookAsync(string sessionId, string title, string errorMsg, string content);
}

public class BusinessAlertProvider : IBusinessAlertProvider, ISingletonDependency
{
    private readonly IHttpClientService _httpClientService;
    private readonly ILogger<BusinessAlertProvider> _logger;
    private readonly BusinessAlertOptions _businessAlertOptions;
    
    public BusinessAlertProvider(
        IHttpClientService httpClientService,
        ILogger<BusinessAlertProvider> logger,
        IOptionsSnapshot<BusinessAlertOptions> businessAlertOptions)
    {
        _httpClientService = httpClientService;
        _logger = logger;
        _businessAlertOptions = businessAlertOptions.Value;
    }
    
    private long _lastSendTime = DateTimeOffset.Now.ToUnixTimeSeconds();
    public async Task SendWebhookAsync(string sessionId, string title, string errorMsg, string content)
    {
        try
        {
            if (DateTimeOffset.Now.ToUnixTimeSeconds() - _lastSendTime < _businessAlertOptions.SendInterval)
            {
                return;
            }

            var msg = new
            {
                msg_type = "text",
                content = new
                    { text = $" title : {title}\n sessionId : {sessionId} \n errorMsg : {errorMsg} \n content : {content}" }
            };
            await _httpClientService.PostAsync<object>(_businessAlertOptions.Webhook, msg);
            _lastSendTime = DateTimeOffset.Now.ToUnixTimeSeconds();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "SendWebhookAsync error, sessionId = {0}, content = {1}", sessionId, content);
        }
    }
}