using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using AElf.Indexing.Elasticsearch;
using CAServer.Entities.Es;
using CAServer.Monitor.Interceptor;
using CAServer.Verifier.Etos;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace CAServer.EntityEventHandler.Core;

public class UserExtraInfoHandler : IDistributedEventHandler<UserExtraInfoEto>, ITransientDependency
{
    private readonly INESTRepository<UserExtraInfoIndex, string> _userExtraInfoRepository;
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<CaAccountHandler> _logger;

    public UserExtraInfoHandler(
        INESTRepository<UserExtraInfoIndex, string> userExtraInfoRepository,
        IObjectMapper objectMapper,
        ILogger<CaAccountHandler> logger)
    {
        _userExtraInfoRepository = userExtraInfoRepository;
        _objectMapper = objectMapper;
        _logger = logger;
    }

    [ExceptionHandler(typeof(Exception),
        Message = "UserExtraInfoHandler UserExtraInfoEto exist error",  
        TargetType = typeof(ExceptionHandlingService), 
        MethodName = nameof(ExceptionHandlingService.HandleExceptionP1))
    ]
    public async Task HandleEventAsync(UserExtraInfoEto eventData)
    {
        var userInfo = _objectMapper.Map<UserExtraInfoEto, UserExtraInfoIndex>(eventData);
        _logger.LogDebug("UserExtraInfoHandler UserExtraInfoEto User extra info add or update: {eventData}", JsonConvert.SerializeObject(userInfo));
        await _userExtraInfoRepository.AddOrUpdateAsync(userInfo);

        _logger.LogDebug($"UserExtraInfoHandler UserExtraInfoEto User extra info add or update success: {JsonConvert.SerializeObject(userInfo)}");
    }
}