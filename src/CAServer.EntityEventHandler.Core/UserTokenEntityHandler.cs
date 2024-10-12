using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using AElf.Indexing.Elasticsearch;
using CAServer.Entities.Es;
using CAServer.Monitor.Interceptor;
using CAServer.Tokens.Etos;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp.EventBus.Distributed;

namespace CAServer.EntityEventHandler.Core;

public class UserTokenEntityHandler : EntityHandlerBase,
    IDistributedEventHandler<UserTokenEto>,
    IDistributedEventHandler<UserTokenDeleteEto>
{
    private readonly INESTRepository<UserTokenIndex, Guid> _userTokenIndexRepository;
    private readonly ILogger<UserTokenEntityHandler> _logger;

    public UserTokenEntityHandler(INESTRepository<UserTokenIndex, Guid> userTokenIndexRepository,
        ILogger<UserTokenEntityHandler> logger)
    {
        _userTokenIndexRepository = userTokenIndexRepository;
        _logger = logger;
    }

    [ExceptionHandler(typeof(Exception),
        Message = "UserTokenEntityHandler UserTokenEto exist error",  
        TargetType = typeof(ExceptionHandlingService), 
        MethodName = nameof(ExceptionHandlingService.HandleExceptionP1))
    ]
    public async Task HandleEventAsync(UserTokenEto eventData)
    {
        _logger.LogInformation("UserTokenEto user token is adding.{userId}-{chainId}-{symbol}", eventData.UserId,
            eventData.Token.ChainId, eventData.Token.Symbol);
        var index = ObjectMapper.Map<UserTokenEto, UserTokenIndex>(eventData);
        await _userTokenIndexRepository.AddOrUpdateAsync(index);
        _logger.LogInformation("UserTokenEto user token add success.{userId}-{chainId}-{symbol}", eventData.UserId,
            eventData.Token.ChainId, eventData.Token.Symbol);
    }

    [ExceptionHandler(typeof(Exception),
        Message = "UserTokenEntityHandler UserTokenDeleteEto exist error",  
        TargetType = typeof(ExceptionHandlingService), 
        MethodName = nameof(ExceptionHandlingService.HandleExceptionP1))
    ]
    public async Task HandleEventAsync(UserTokenDeleteEto eventData)
    {
        _logger.LogInformation("UserTokenDeleteEto user token is deleting.{userId}-{chainId}-{symbol}", eventData.UserId,
            eventData.Token.ChainId, eventData.Token.Symbol);
        
        await _userTokenIndexRepository.DeleteAsync(eventData.Id);
        _logger.LogInformation("UserTokenDeleteEto user token delete success.{userId}-{chainId}-{symbol}", eventData.UserId,
            eventData.Token.ChainId, eventData.Token.Symbol);

    }
}