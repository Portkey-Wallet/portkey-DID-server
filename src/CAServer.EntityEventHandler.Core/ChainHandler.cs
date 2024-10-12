using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using AElf.Indexing.Elasticsearch;
using CAServer.Entities.Es;
using CAServer.Etos.Chain;
using CAServer.Monitor.Interceptor;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace CAServer.EntityEventHandler.Core;

public class ChainHandler : IDistributedEventHandler<ChainCreateEto>,
    IDistributedEventHandler<ChainUpdateEto>,
    IDistributedEventHandler<ChainDeleteEto>,
    ITransientDependency
{
    private readonly INESTRepository<ChainsInfoIndex, string> _chainsInfoRepository;
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<ChainHandler> _logger;

    public ChainHandler(INESTRepository<ChainsInfoIndex, string> chainsInfoRepository,
        IObjectMapper objectMapper,
        ILogger<ChainHandler> logger)
    {
        _chainsInfoRepository = chainsInfoRepository;
        _objectMapper = objectMapper;
        _logger = logger;
    }

    [ExceptionHandler(typeof(Exception),
        Message = "ChainHandler HandleEventAsync exist error",  
        TargetType = typeof(ExceptionHandlingService), 
        MethodName = nameof(ExceptionHandlingService.HandleExceptionP1))
    ]
    public async Task HandleEventAsync(ChainCreateEto eventData)
    {
        await _chainsInfoRepository.AddAsync(_objectMapper.Map<ChainCreateEto, ChainsInfoIndex>(eventData));
        _logger.LogDebug($"ChainCreateEto Chain info add success: {JsonConvert.SerializeObject(eventData)}");
    }

    [ExceptionHandler(typeof(Exception),
        Message = "ChainHandler ChainUpdateEto exist error",  
        TargetType = typeof(ExceptionHandlingService), 
        MethodName = nameof(ExceptionHandlingService.HandleExceptionP1))
    ]
    public async Task HandleEventAsync(ChainUpdateEto eventData)
    {
        await _chainsInfoRepository.UpdateAsync(_objectMapper.Map<ChainUpdateEto, ChainsInfoIndex>(eventData));
        _logger.LogDebug($"ChainUpdateEto chain info update success: {JsonConvert.SerializeObject(eventData)}");
    }

    [ExceptionHandler(typeof(Exception),
        Message = "ChainHandler ChainDeleteEto exist error",  
        TargetType = typeof(ExceptionHandlingService), 
        MethodName = nameof(ExceptionHandlingService.HandleExceptionP1))
    ]
    public async Task HandleEventAsync(ChainDeleteEto eventData)
    {
        await _chainsInfoRepository.DeleteAsync(_objectMapper.Map<ChainDeleteEto, ChainsInfoIndex>(eventData));
        _logger.LogDebug("ChainDeleteEto chain info delete success");
    }
}