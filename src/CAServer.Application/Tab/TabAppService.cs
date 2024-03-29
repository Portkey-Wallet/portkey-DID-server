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
    private const string _cachePrefix = "CaServer:TabCommunication";

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
            return;
        }

        await _distributedEvent.PublishAsync(ObjectMapper.Map<TabCompleteDto, TabCompleteEto>(input));
        Logger.LogInformation("tab communication published, clientId:{clientId}, methodName:{methodName}",
            input.ClientId, input.MethodName);
    }

    private async Task SaveAsync(TabCompleteDto input)
    {
        await _distributedCache.SetAsync(GetKey($"{input.ClientId}:{input.MethodName}"), new TabCompleteInfo()
            {
                ClientId = input.ClientId,
                MethodName = input.MethodName,
                Data = input.Data
            },
            new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = DateTimeOffset.UtcNow.AddDays(_options.ExpireDays)
            });
    }

    private string GetKey(string key)
    {
        if (key.Contains(CommonConstant.Hyphen) || key.Contains(CommonConstant.Underline))
        {
            key = key.Replace(CommonConstant.Hyphen, CommonConstant.Colon)
                .Replace(CommonConstant.Underline, CommonConstant.Colon);
        }

        return $"{_cachePrefix}:{key}";
    }
}