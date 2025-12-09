using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.Notify.Etos;
using CAServer.Entities.Es;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace CAServer.EntityEventHandler.Core;

public class NotifyHandler : IDistributedEventHandler<NotifyEto>,
    IDistributedEventHandler<DeleteNotifyEto>, ITransientDependency
{
    private readonly INESTRepository<NotifyRulesIndex, Guid> _notifyRulesRepository;
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<NotifyHandler> _logger;

    public NotifyHandler(INESTRepository<NotifyRulesIndex, Guid> notifyRulesRepository,
        IObjectMapper objectMapper,
        ILogger<NotifyHandler> logger)
    {
        _notifyRulesRepository = notifyRulesRepository;
        _objectMapper = objectMapper;
        _logger = logger;
    }

    public async Task HandleEventAsync(NotifyEto eventData)
    {
        try
        {
            await _notifyRulesRepository.AddOrUpdateAsync(_objectMapper.Map<NotifyEto, NotifyRulesIndex>(eventData));
            _logger.LogDebug("NotifyRules add or update success: {EventData}", JsonConvert.SerializeObject(eventData));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Message}", JsonConvert.SerializeObject(eventData));
        }
    }

    public async Task HandleEventAsync(DeleteNotifyEto eventData)
    {
        try
        {
            await _notifyRulesRepository.DeleteAsync(_objectMapper.Map<DeleteNotifyEto, NotifyRulesIndex>(eventData));
            _logger.LogDebug("NotifyRules delete success: {EventData}", JsonConvert.SerializeObject(eventData));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Message}", JsonConvert.SerializeObject(eventData));
        }
    }
}