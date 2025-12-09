using System;
using System.Threading.Tasks;
using CAServer.Commons;
using CAServer.Options;
using CAServer.Tab.Dtos;
using CAServer.Tab.Etos;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.Caching;
using Volo.Abp.EventBus.Distributed;

namespace CAServer.Tab;

[RemoteService(false), DisableAuditing]
public class TabAppService : CAServerAppService, ITabAppService
{
    private readonly IDistributedEventBus _distributedEvent;
    private readonly IDistributedCache<TabCompleteInfo> _distributedCache;
    private readonly HubConfigOptions _options;

    public TabAppService(IDistributedEventBus distributedEvent, IDistributedCache<TabCompleteInfo> distributedCache,
        IOptionsSnapshot<HubConfigOptions> options)
    {
        _distributedEvent = distributedEvent;
        _distributedCache = distributedCache;
        _options = options.Value;
    }

    public async Task CompleteAsync(TabCompleteDto input)
    {
        if (input.NeedPersist)
        {
            await SaveAsync(input);
            Logger.LogInformation(
                "tab communication info save success, clientId:{clientId}, methodName:{methodName}, targetClientId:{targetClientId}",
                input.ClientId, input.MethodName, input.TargetClientId ?? string.Empty);
        }

        await _distributedEvent.PublishAsync(ObjectMapper.Map<TabCompleteDto, TabCompleteEto>(input));
        Logger.LogInformation(
            "tab communication published, clientId:{clientId}, methodName:{methodName}, targetClientId:{targetClientId}",
            input.ClientId, input.MethodName, input.TargetClientId ?? string.Empty);
    }

    private async Task SaveAsync(TabCompleteDto input)
    {
        await _distributedCache.SetAsync(HubCacheHelper.GetTabKey($"{input.ClientId}:{input.MethodName}"),
            new TabCompleteInfo()
            {
                ClientId = input.ClientId,
                MethodName = input.MethodName,
                Data = input.Data,
                TargetClientId = input.TargetClientId
            },
            new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = DateTimeOffset.UtcNow.AddDays(_options.ExpireDays)
            });
    }
}