using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using AElf.Indexing.Elasticsearch;
using CAServer.Entities.Es;
using CAServer.Monitor.Interceptor;
using CAServer.ZeroHoldings.Etos;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp.EventBus.Distributed;

namespace CAServer.EntityEventHandler.Core;

public class ZeroHoldingsConfigEntityHandler : EntityHandlerBase,
    IDistributedEventHandler<ZeroHoldingsConfigEto>
{
    private readonly INESTRepository<ZeroHoldingsConfigIndex, Guid> _repository;
    private readonly ILogger<ZeroHoldingsConfigEntityHandler> _logger;

    public ZeroHoldingsConfigEntityHandler(INESTRepository<ZeroHoldingsConfigIndex, Guid> repository,
        ILogger<ZeroHoldingsConfigEntityHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    [ExceptionHandler(typeof(Exception),
        Message = "ZeroHoldingsConfig exist error",  
        TargetType = typeof(ExceptionHandlingService), 
        MethodName = nameof(ExceptionHandlingService.HandleExceptionP1))
    ]
    public async Task HandleEventAsync(ZeroHoldingsConfigEto eventData)
    {
        _logger.LogInformation($"[ZeroHoldingsConfig] HandleEventAsync eventData : {JsonConvert.SerializeObject(eventData)} ");
        var index = ObjectMapper.Map<ZeroHoldingsConfigEto, ZeroHoldingsConfigIndex>(eventData);
        await _repository.AddOrUpdateAsync(index);
        _logger.LogInformation($"[ZeroHoldingsConfig] HandleEventAsync eventData : {JsonConvert.SerializeObject(eventData)} done");

    }
}