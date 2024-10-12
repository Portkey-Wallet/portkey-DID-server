using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using AElf.Indexing.Elasticsearch;
using CAServer.Entities.Es;
using CAServer.Guardian;
using CAServer.Monitor.Interceptor;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace CAServer.EntityEventHandler.Core;

public class GuardianHandler : IDistributedEventHandler<GuardianEto>, IDistributedEventHandler<GuardianDeleteEto>,
    ITransientDependency
{
    private readonly INESTRepository<GuardianIndex, string> _guardianRepository;
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<CaAccountHandler> _logger;

    public GuardianHandler(
        INESTRepository<GuardianIndex, string> guardianRepository,
        IObjectMapper objectMapper,
        ILogger<CaAccountHandler> logger)
    {
        _guardianRepository = guardianRepository;
        _objectMapper = objectMapper;
        _logger = logger;
    }

    [ExceptionHandler(typeof(Exception),
        Message = "GrowthHandler GuardianEto exist error",  
        TargetType = typeof(ExceptionHandlingService), 
        MethodName = nameof(ExceptionHandlingService.HandleExceptionP1))
    ]
    public async Task HandleEventAsync(GuardianEto eventData)
    {
        var guardian = _objectMapper.Map<GuardianEto, GuardianIndex>(eventData);
        await _guardianRepository.AddOrUpdateAsync(guardian);
            
        _logger.LogDebug("GuardianEto add or update success, id: {id}", eventData.Id);
    }

    [ExceptionHandler(typeof(Exception),
        Message = "GrowthHandler GuardianDeleteEto exist error",  
        TargetType = typeof(ExceptionHandlingService), 
        MethodName = nameof(ExceptionHandlingService.HandleExceptionP1))
    ]
    public async Task HandleEventAsync(GuardianDeleteEto eventData)
    {
        var guardian = _objectMapper.Map<GuardianDeleteEto, GuardianIndex>(eventData);
        await _guardianRepository.UpdateAsync(guardian);
            
        _logger.LogDebug("GuardianDeleteEto Guardian delete success, id: {id}", eventData.Id);
    }
}