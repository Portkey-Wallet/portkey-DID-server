using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.ContractEventHandler.Core.Application;
using CAServer.Guardian;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace CAServer.ContractEventHandler.Core;

public class CAZkLoginContractEventHandler: IDistributedEventHandler<ZkEto>, ITransientDependency
{
    private readonly IZkloginPoseidongHashService _zklogin;
    private readonly ILogger<CAZkLoginContractEventHandler> _logger;

    public CAZkLoginContractEventHandler(IZkloginPoseidongHashService zklogin,
        ILogger<CAZkLoginContractEventHandler> logger)
    {
        _zklogin = zklogin;
        _logger = logger;
    }
    
    public async Task HandleEventAsync(ZkEto eventData)
    {
        _logger.LogInformation("receive zkEto message:{0}", JsonConvert.SerializeObject(eventData));
        await _zklogin.DoWorkAsync(new List<string>(){eventData.CaHash});
    }
}