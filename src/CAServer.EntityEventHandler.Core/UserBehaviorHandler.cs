using System;
using System.Threading.Tasks;
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

    public async Task HandleEventAsync(UserBehaviorEto eventData)
    {
        try
        {
            _ = _userBehaviorAppService.AddUserBehaviorAsync(eventData);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to handle user behavior event.");
        }
    }
}