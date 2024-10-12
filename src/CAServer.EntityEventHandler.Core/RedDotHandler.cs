using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using AElf.Indexing.Elasticsearch;
using CAServer.Entities.Es;
using CAServer.Monitor.Interceptor;
using CAServer.RedDot.Etos;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace CAServer.EntityEventHandler.Core;

public class RedDotHandler : IDistributedEventHandler<RedDotEto>, ITransientDependency
{
    private readonly INESTRepository<RedDotIndex, string> _redDotRepository;
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<RedDotHandler> _logger;

    public RedDotHandler(INESTRepository<RedDotIndex, string> redDotRepository,
        IObjectMapper objectMapper,
        ILogger<RedDotHandler> logger)
    {
        _redDotRepository = redDotRepository;
        _objectMapper = objectMapper;
        _logger = logger;
    }

    [ExceptionHandler(typeof(Exception),
        Message = "RedDotHandler HandleEventAsync exist error",  
        TargetType = typeof(ExceptionHandlingService), 
        MethodName = nameof(ExceptionHandlingService.HandleExceptionP1))
    ]
    public async Task HandleEventAsync(RedDotEto eventData)
    {
        await _redDotRepository.AddAsync(_objectMapper.Map<RedDotEto, RedDotIndex>(eventData));
        _logger.LogInformation(
            "RedDotHandler HandleEventAsync red dot info add or update success, redDotInfo:{redDotInfo}", JsonConvert.SerializeObject(eventData));

    }
}