using System.Threading.Tasks;
using CAServer.Tab.Dtos;
using CAServer.Tab.Etos;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.EventBus.Distributed;

namespace CAServer.Tab;

[RemoteService(false), DisableAuditing]
public class TabAppService : CAServerAppService, ITabAppService
{
    private readonly IDistributedEventBus _distributedEvent;

    public TabAppService(IDistributedEventBus distributedEvent)
    {
        _distributedEvent = distributedEvent;
    }

    public async Task CompleteAsync(TabCompleteDto input)
    {
        await _distributedEvent.PublishAsync(ObjectMapper.Map<TabCompleteDto, TabCompleteEto>(input));
        Logger.LogInformation("tab communication published, clientId:{clientId}, methodName:{methodName}",
            input.ClientId, input.MethodName);
    }
}