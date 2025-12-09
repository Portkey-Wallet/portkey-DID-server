using System;
using System.Threading.Tasks;
using CAServer.Commons;
using CAServer.ContractEventHandler.Core.Application;
using CAServer.Etos;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace CAServer.ContractEventHandler.Core;

public class CAContractEventHandler : IDistributedEventHandler<AccountRegisterCreateEto>,
    IDistributedEventHandler<AccountRecoverCreateEto>, ITransientDependency
{
    private readonly IContractAppService _contractAppService;
    private readonly ILogger<CAContractEventHandler> _logger;

    public CAContractEventHandler(IContractAppService contractAppService, ILogger<CAContractEventHandler> logger)
    {
        _contractAppService = contractAppService;
        _logger = logger;
    }

    public async Task HandleEventAsync(AccountRegisterCreateEto eventData)
    {
        try
        {
            // CreateHolderInfo can take a long time
            _ = _contractAppService.CreateHolderInfoAsync(eventData);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "RegisterMessage Error: {eventData}", JsonConvert.SerializeObject(eventData));
        }
    }

    public async Task HandleEventAsync(AccountRecoverCreateEto eventData)
    {
        try
        {
            // SocialRecovery can take a long time
            _ = _contractAppService.SocialRecoveryAsync(eventData);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "RecoveryMessage Error: {eventData}", JsonConvert.SerializeObject(eventData));
        }
    }
}