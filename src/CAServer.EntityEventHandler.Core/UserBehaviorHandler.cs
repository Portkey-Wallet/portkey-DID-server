using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using CAServer.Monitor.Interceptor;
using CAServer.UserBehavior;
using CAServer.UserBehavior.Etos;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace CAServer.EntityEventHandler.Core;

public class UserBehaviorHandler : IDistributedEventHandler<UserBehaviorEto>, ITransientDependency
{
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<UserBehaviorHandler> _logger;
    private readonly IUserBehaviorAppService _userBehaviorAppService;
    
    public UserBehaviorHandler(
        IObjectMapper objectMapper,
        ILogger<UserBehaviorHandler> logger,
        IUserBehaviorAppService userBehaviorAppService)
    {
        _objectMapper = objectMapper;
        _logger = logger;
        _userBehaviorAppService = userBehaviorAppService;
    }

    [ExceptionHandler(typeof(Exception),
        Message = "UserBehaviorHandler UserBehaviorEto exist error",  
        TargetType = typeof(ExceptionHandlingService), 
        MethodName = nameof(ExceptionHandlingService.HandleExceptionP1))
    ]
    public async Task HandleEventAsync(UserBehaviorEto eventData)
    {
        await _userBehaviorAppService.AddUserBehaviorAsync(eventData);
    }
}