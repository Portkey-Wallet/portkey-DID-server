using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using AElf.Indexing.Elasticsearch;
using CAServer.Notify.Etos;
using CAServer.Entities.Es;
using CAServer.Monitor.Interceptor;
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

    [ExceptionHandler(typeof(Exception),
        Message = "NotifyHandler NotifyEto exist error",  
        TargetType = typeof(ExceptionHandlingService), 
        MethodName = nameof(ExceptionHandlingService.HandleExceptionP1))
    ]
    public async Task HandleEventAsync(NotifyEto eventData)
    {
        await _notifyRulesRepository.AddOrUpdateAsync(_objectMapper.Map<NotifyEto, NotifyRulesIndex>(eventData));
        _logger.LogDebug("NotifyHandler NotifyEto add or update success: {EventData}", JsonConvert.SerializeObject(eventData));
    }

    [ExceptionHandler(typeof(Exception),
        Message = "NotifyHandler NotifyEto exist error",  
        TargetType = typeof(ExceptionHandlingService), 
        MethodName = nameof(ExceptionHandlingService.HandleExceptionP1))
    ]
    public async Task HandleEventAsync(DeleteNotifyEto eventData)
    {
        await _notifyRulesRepository.DeleteAsync(_objectMapper.Map<DeleteNotifyEto, NotifyRulesIndex>(eventData));
        _logger.LogDebug("NotiNotifyHandler DeleteNotifyEto delete success: {EventData}", JsonConvert.SerializeObject(eventData));
    }
}