using System;
using System.Threading.Tasks;
using CAServer.Etos;
using CAServer.UserAssets;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace CAServer.EntityEventHandler.Core;

public class UserLoginHandler : IDistributedEventHandler<UserLoginEto>,ITransientDependency
{
    private readonly IUserAssetsAppService _userAssetsAppService;
    private readonly ILogger<UserLoginHandler> _logger;
    
    public UserLoginHandler(IUserAssetsAppService userAssetsAppService, ILogger<UserLoginHandler> logger)
    {
        _userAssetsAppService = userAssetsAppService;
        _logger = logger;
    }
    
    public async Task HandleEventAsync(UserLoginEto eventData)
    {
        try 
        {
            await _userAssetsAppService.SyncOriginChainIdAsync(eventData);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "UserLoginHandler HandleEventAsync error");
        }
    }
}