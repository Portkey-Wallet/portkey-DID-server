using System;
using System.Threading.Tasks;
using CAServer.CAAccount.Dtos;
using CAServer.Dtos;
using CAServer.Etos;
using CAServer.Hubs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace CAServer.HubsEventHandler;

public class CAAccountHandler : IDistributedEventHandler<AccountRegisterCompletedEto>,
    IDistributedEventHandler<AccountRecoverCompletedEto>,
    ITransientDependency
{
    private readonly IHubProvider _caHubProvider;
    private readonly ILogger<CAAccountHandler> _logger;

    public CAAccountHandler(
        IHubProvider caHubProvider,
        ILogger<CAAccountHandler> logger)
    {
        _caHubProvider = caHubProvider;
        _logger = logger;
    }

    public async Task HandleEventAsync(AccountRegisterCompletedEto eventData)
    {
        try
        {
            _logger.LogDebug("send message to client.");
            await _caHubProvider.ResponseAsync(new HubResponse<RegisterCompletedMessageDto>
            {
                RequestId = eventData.Context.RequestId,
                Body = eventData.RegisterCompletedMessage
            }, eventData.Context.ClientId, method: "caAccountRegister");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Message}", JsonConvert.SerializeObject(eventData));
        }
    }

    public async Task HandleEventAsync(AccountRecoverCompletedEto eventData)
    {
        try
        {
            _logger.LogDebug("send message to client.");
            await _caHubProvider.ResponseAsync(new HubResponse<RecoveryCompletedMessageDto>
            {
                RequestId = eventData.Context.RequestId,
                Body = eventData.RecoveryCompletedMessage
            }, eventData.Context.ClientId, method: "caAccountRecover");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Message}", JsonConvert.SerializeObject(eventData));
        }
    }
}